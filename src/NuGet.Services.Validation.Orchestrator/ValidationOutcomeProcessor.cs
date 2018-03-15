// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Services.Validation.Orchestrator.Telemetry;
using NuGetGallery;

namespace NuGet.Services.Validation.Orchestrator
{
    public class ValidationOutcomeProcessor : IValidationOutcomeProcessor
    {
        private readonly IValidationStorageService _validationStorageService;
        private readonly IPackageValidationEnqueuer _validationEnqueuer;
        private readonly IPackageStatusProcessor _packageStateProcessor;
        private readonly IValidationPackageFileService _packageFileService;
        private readonly ValidationConfiguration _validationConfiguration;
        private readonly Dictionary<string, ValidationConfigurationItem> _validationConfigurationsByName;
        private readonly IMessageService _messageService;
        private readonly ITelemetryService _telemetryService;
        private readonly ILogger<ValidationOutcomeProcessor> _logger;

        public ValidationOutcomeProcessor(
            IValidationStorageService validationStorageService,
            IPackageValidationEnqueuer validationEnqueuer,
            IPackageStatusProcessor validatedPackageProcessor,
            IValidationPackageFileService packageFileService,
            IOptionsSnapshot<ValidationConfiguration> validationConfigurationAccessor,
            IMessageService messageService,
            ITelemetryService telemetryService,
            ILogger<ValidationOutcomeProcessor> logger)
        {
            _validationStorageService = validationStorageService ?? throw new ArgumentNullException(nameof(validationStorageService));
            _validationEnqueuer = validationEnqueuer ?? throw new ArgumentNullException(nameof(validationEnqueuer));
            _packageStateProcessor = validatedPackageProcessor ?? throw new ArgumentNullException(nameof(validatedPackageProcessor));
            _packageFileService = packageFileService ?? throw new ArgumentNullException(nameof(packageFileService));
            if (validationConfigurationAccessor == null)
            {
                throw new ArgumentNullException(nameof(validationConfigurationAccessor));
            }
            _validationConfiguration = validationConfigurationAccessor.Value 
                ?? throw new ArgumentException($"The {nameof(validationConfigurationAccessor)}.Value property cannot be null",
                    nameof(validationConfigurationAccessor));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _validationConfigurationsByName = _validationConfiguration.Validations.ToDictionary(v => v.Name);
        }

        public async Task ProcessValidationOutcomeAsync(PackageValidationSet validationSet, IValidatable validatable)
        {
            var failedValidations = GetFailedValidations(validationSet);

            if (failedValidations.Any())
            {
                _logger.LogWarning("Some validations failed for package {Validatable}, validation set {ValidationSetId}: {FailedValidations}",
                    validatable.ToString(),
                    validationSet.ValidationTrackingId,
                    failedValidations.Select(x => x.Type).ToList());

                await validatable.OnValidationSetFailed(validationSet);
            }
            else if (AllValidationsSucceeded(validationSet))
            {
                _logger.LogInformation("All validations are complete for the package {Validatable}, validation set {ValidationSetId}",
                    validatable.ToString(),
                    validationSet.ValidationTrackingId);

                await validatable.OnValidationSetSucceeded(validationSet);
            }
            else
            {
                await validatable.OnInProgressValidation(validationSet);
            }
        }

        private async Task CompleteValidationSetAsync(Package package, PackageValidationSet validationSet, bool isSuccess)
        {
            await _packageFileService.DeletePackageForValidationSetAsync(validationSet);

            _logger.LogInformation("Done processing {PackageId} {PackageVersion} {ValidationSetId} with IsSuccess = {IsSuccess}.",
                package.PackageRegistration.Id,
                package.NormalizedVersion,
                validationSet.ValidationTrackingId,
                isSuccess);

            TrackTotalValidationDuration(validationSet, isSuccess);
        }

        private ValidationConfigurationItem GetValidationConfigurationItemByName(string name)
        {
            _validationConfigurationsByName.TryGetValue(name, out var item);

            return item;
        }

        private void TrackTotalValidationDuration(PackageValidationSet validationSet, bool isSuccess)
        {
            _telemetryService.TrackTotalValidationDuration(
                DateTime.UtcNow - validationSet.Created,
                isSuccess);
        }

        private bool AllValidationsSucceeded(PackageValidationSet packageValidationSet)
        {
            return packageValidationSet
                .PackageValidations
                .All(pv => pv.ValidationStatus == ValidationStatus.Succeeded
                    || GetValidationConfigurationItemByName(pv.Type)?.FailureBehavior == ValidationFailureBehavior.AllowedToFail);
        }

        private List<PackageValidation> GetFailedValidations(PackageValidationSet packageValidationSet)
        {
            return packageValidationSet
                .PackageValidations
                .Where(v => v.ValidationStatus == ValidationStatus.Failed)
                .Where(v => GetValidationConfigurationItemByName(v.Type)?.FailureBehavior == ValidationFailureBehavior.MustSucceed)
                .ToList();
        }

        private List<PackageValidation> GetIncompleteTimedOutValidations(PackageValidationSet packageValidationSet)
        {
            bool IsPackageValidationTimedOut(PackageValidation validation)
            {
                var config = GetValidationConfigurationItemByName(validation.Type);
                var duration = DateTime.UtcNow - validation.Started;

                return duration > config?.TrackAfter;
            }

            return packageValidationSet
                .PackageValidations
                .Where(v => v.ValidationStatus == ValidationStatus.Incomplete)
                .Where(IsPackageValidationTimedOut)
                .ToList();
        }
    }
}
