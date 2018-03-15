// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Services.Validation.Orchestrator.Telemetry;
using NuGetGallery;

namespace NuGet.Services.Validation.Orchestrator
{
    public class ValidationSetProvider : IValidationSetProvider
    {
        private readonly IValidationStorageService _validationStorageService;
        private readonly IValidationPackageFileService _packageFileService;
        private readonly ValidationConfiguration _validationConfiguration;
        private readonly ITelemetryService _telemetryService;
        private readonly ILogger<ValidationSetProvider> _logger;

        public ValidationSetProvider(
            IValidationStorageService validationStorageService,
            IValidationPackageFileService packageFileService,
            IOptionsSnapshot<ValidationConfiguration> validationConfigurationAccessor,
            ITelemetryService telemetryService,
            ILogger<ValidationSetProvider> logger)
        {
            _validationStorageService = validationStorageService ?? throw new ArgumentNullException(nameof(validationStorageService));
            _packageFileService = packageFileService ?? throw new ArgumentNullException(nameof(packageFileService));
            if (validationConfigurationAccessor == null)
            {
                throw new ArgumentNullException(nameof(validationConfigurationAccessor));
            }
            _validationConfiguration = validationConfigurationAccessor.Value ?? throw new ArgumentException($"The Value property cannot be null", nameof(validationConfigurationAccessor));
            _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PackageValidationSet> TryGetOrCreateValidationSetAsync(Guid validationTrackingId, IValidatable validatable)
        {
            var validationSet = await _validationStorageService.GetValidationSetAsync(validationTrackingId);

            if (validationSet == null)
            {
                var shouldSkip = await _validationStorageService.OtherRecentValidationSetForPackageExists(
                    validatable.EntityType,
                    validatable.EntityKey,
                    _validationConfiguration.NewValidationRequestDeduplicationWindow,
                    validationTrackingId);
                if (shouldSkip)
                {
                    return null;
                }

                validationSet = InitializeValidationSet(validationTrackingId, validatable);

                await validatable.EnrichNewValidationSet(validationSet);

                validationSet = await PersistValidationSetAsync(validationSet, validatable);
            }
            else
            {
                await validatable.EnsureValidationSetMatches(validationSet);
            }

            return validationSet;
        }

        private async Task<PackageValidationSet> PersistValidationSetAsync(PackageValidationSet validationSet, IValidatable validatable)
        {
            _logger.LogInformation("Persisting validation set {ValidationSetId} for {Validatable}",
                validationSet.ValidationTrackingId,
                validatable.ToString());

            var persistedValidationSet = await _validationStorageService.CreateValidationSetAsync(validationSet);

            await validatable.OnFirstValidationCreated(validationSet);

            return persistedValidationSet;
        }

        private PackageValidationSet InitializeValidationSet(Guid validationTrackingId, IValidatable validatable)
        {
            _logger.LogInformation("Initializing validation set {ValidationSetId} for {Validatable})",
                validationTrackingId,
                validatable.ToString());

            var now = DateTime.UtcNow;

            var validationSet = new PackageValidationSet
            {
                Created = now,
                PackageKey = validatable.EntityKey,
                PackageValidations = new List<PackageValidation>(),
                Updated = now,
                ValidationTrackingId = validationTrackingId,
            };

            foreach (var validation in _validationConfiguration.Validations)
            {
                var packageValidation = new PackageValidation
                {
                    PackageValidationSet = validationSet,
                    ValidationStatus = ValidationStatus.NotStarted,
                    Type = validation.Name,
                    ValidationStatusTimestamp = now,
                };

                validationSet.PackageValidations.Add(packageValidation);
            }

            return validationSet;
        }
    }
}
