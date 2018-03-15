// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Services.Validation.Orchestrator.Telemetry;
using NuGetGallery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Validation.Orchestrator
{
    public class PackageValidatable : IValidatable
    {
        private Package _package;
        private readonly IValidationPackageFileService _packageFileService;
        private readonly ITelemetryService _telemetryService;
        private readonly IValidationStorageService _validationStorageService;

        public PackageValidatable(
            IValidationPackageFileService packageFileService,
            ITelemetryService telemetryService,
            IValidationStorageService validationStorageService)
        {
            _packageFileService = packageFileService ?? throw new ArgumentNullException(nameof(packageFileService));
            _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
            _validationStorageService = validationStorageService ?? throw new ArgumentNullException(nameof(validationStorageService));
        }

        public int EntityKey => _package.Key;

        public string EntityType => "Package";

        public async Task EnrichNewValidationSet(PackageValidationSet validationSet)
        {
            if (_package.PackageStatusKey == PackageStatus.Available)
            {
                var packageETag = await _packageFileService.CopyPackageFileForValidationSetAsync(validationSet);

                // This indicates that the package in the package container is expected to not change.
                validationSet.PackageETag = packageETag;
            }
            else
            {
                await _packageFileService.CopyValidationPackageForValidationSetAsync(validationSet);

                // This indicates that the package in the packages container is expected to not exist (i.e. it has
                // has no etag at all).
                validationSet.PackageETag = null;
            }
        }

        public override string ToString()
        {
            return $"{{Nupkg: {_package.PackageRegistration.Id} {_package.NormalizedVersion} {_package.Key}}}";
        }

        public void SetPackage(Package package)
        {
            _package = package;
        }

        public async Task OnFirstValidationCreated(PackageValidationSet validationSet)
        {
            // Only track the validation set creation time when this is the first validation set to be created for that
            // package. There will be more than one validation set when an admin has requested a manual revalidation.
            // This can happen much later than when the package was created so the duration is less interesting in that
            // case.
            if (await _validationStorageService.GetValidationSetCountAsync(EntityType, EntityKey) == 1)
            {
                _telemetryService.TrackDurationToValidationSetCreation(validationSet.Created - _package.Created);
            }
        }

        public Task EnsureValidationSetMatches(PackageValidationSet validationSet)
        {
            var sameKey = EntityKey == validationSet.PackageKey && EntityType == "validationset entity type"; // TODO

            if (!sameKey)
            {
                throw new InvalidOperationException($"Validation set package key ({validationSet.PackageKey}) " +
                    $"does not match expected package key ({EntityKey}).");
            }

            return Task.CompletedTask;
        }

        public Task<IValidator> GetValidator(string validatorName)
        {
            // probably a bit of reflection magic
            throw new NotImplementedException();
        }

        public Task OnValidationSetFailed(PackageValidationSet validationSet)
        {
            // The only way we can move to the failed validation state is if the package is currently in the
            // validating state. This has a beneficial side effect of only sending a failed validation email to the
            // customer when the package first moves to the failed validation state. If an admin comes along and
            // revalidates the package and the package fails validation again, we don't want another email going
            // out since that would be noisy for the customer.                
            if (package.PackageStatusKey == PackageStatus.Validating)
            {
                await _packageStateProcessor.SetPackageStatusAsync(package, validationSet, PackageStatus.FailedValidation);

                var issuesExistAndAllPackageSigned = validationSet
                    .PackageValidations
                    .SelectMany(pv => pv.PackageValidationIssues)
                    .Select(pvi => pvi.IssueCode == ValidationIssueCode.PackageIsSigned)
                    .DefaultIfEmpty(false)
                    .All(v => v);

                if (issuesExistAndAllPackageSigned)
                {
                    _messageService.SendPackageSignedValidationFailedMessage(package);
                }
                else
                {
                    _messageService.SendPackageValidationFailedMessage(package);
                }
            }
            else
            {
                // The case when validation fails while PackageStatus not validating is the case of 
                // manual revalidation. In this case we don't want to take package down automatically
                // and let the person who requested revalidation to decide how to proceed. Ops will be
                // alerted by failed validation monitoring.
                _logger.LogInformation("Package {PackageId} {PackageVersion} was {PackageStatus} when validation set {ValidationSetId} failed. Will not mark it as failed.",
                    package.PackageRegistration.Id,
                    package.NormalizedVersion,
                    package.PackageStatusKey,
                    validationSet.ValidationTrackingId);
            }

            await CompleteValidationSetAsync(package, validationSet, isSuccess: false);
        }

        public Task OnValidationSetSucceeded(PackageValidationSet validationSet)
        {
            var fromStatus = package.PackageStatusKey;

            // Always set the package status to available so that processors can have a change to fix packages
            // that are already available. Processors should no-op when their work is already done, so the
            // modification of an already available package should be rare. The most common case for this is if
            // the processor has never been run on a package that was published before the processor was
            // implemented. In this case, the processor has to play catch-up.
            await _packageStateProcessor.SetPackageStatusAsync(package, validationSet, PackageStatus.Available);

            // Only send the email when first transitioning into the Available state.
            if (fromStatus != PackageStatus.Available)
            {
                _messageService.SendPackagePublishedMessage(package);
            }

            await CompleteValidationSetAsync(package, validationSet, isSuccess: true);
        }

        public Task OnInProgressValidation(PackageValidationSet validationSet)
        {
            // There are no failed validations and some validations are still in progress. Update
            // the validation set's Updated field and send a notice if the validation set is taking
            // too long to complete.
            var previousUpdateTime = validationSet.Updated;

            await _validationStorageService.UpdateValidationSetAsync(validationSet);

            var validationSetDuration = validationSet.Updated - validationSet.Created;
            var previousDuration = previousUpdateTime - validationSet.Created;

            // Only send a "validating taking too long" notice once. This is ensured by verifying this is
            // the package's first validation set and that this is the first time the validation set duration
            // is greater than the configured threshold. Service Bus message duplication for a single validation
            // set will not cause multiple notices to be sent due to the row version on PackageValidationSet.
            if (validationSetDuration > _validationConfiguration.ValidationSetNotificationTimeout &&
                previousDuration <= _validationConfiguration.ValidationSetNotificationTimeout &&
                await _validationStorageService.GetValidationSetCountAsync(package.Key) == 1)
            {
                _messageService.SendPackageValidationTakingTooLongMessage(package);
                _telemetryService.TrackSentValidationTakingTooLongMessage(package.PackageRegistration.Id, package.NormalizedVersion, validationSet.ValidationTrackingId);
            }

            // Track any validations that have timed out.
            var timedOutValidations = GetIncompleteTimedOutValidations(validationSet);

            if (timedOutValidations.Any())
            {
                foreach (var validation in timedOutValidations)
                {
                    var duration = DateTime.UtcNow - validation.Started;

                    _logger.LogWarning("Validation {Validation} for package {PackageId} {PackageVersion} has reached the configured failure timeout after duration {Duration}",
                        validation.Type,
                        validationSet.PackageId,
                        validationSet.PackageNormalizedVersion,
                        duration);

                    _telemetryService.TrackValidatorTimeout(validation.Type);
                }
            }

            // Schedule another check if we haven't reached the validation set timeout yet.
            if (validationSetDuration <= _validationConfiguration.TimeoutValidationSetAfter)
            {
                var messageData = new PackageValidationMessageData(package.PackageRegistration.Id, package.Version, validationSet.ValidationTrackingId);
                var postponeUntil = DateTimeOffset.UtcNow + _validationConfiguration.ValidationMessageRecheckPeriod;

                await _validationEnqueuer.StartValidationAsync(messageData, postponeUntil);
            }
            else
            {
                _telemetryService.TrackValidationSetTimeout(package.PackageRegistration.Id, package.NormalizedVersion, validationSet.ValidationTrackingId);
            }
        }
    }
}
