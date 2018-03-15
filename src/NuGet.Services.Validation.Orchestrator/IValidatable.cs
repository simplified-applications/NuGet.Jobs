// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Validation.Orchestrator
{
    public interface IValidatable
    {
        int EntityKey { get; }
        string EntityType { get; }

        Task EnrichNewValidationSet(PackageValidationSet validationSet);
        Task OnFirstValidationCreated(PackageValidationSet validationSet);
        Task EnsureValidationSetMatches(PackageValidationSet validationSet);
        Task<IValidator> GetValidator(string validatorName);
        Task OnValidationSetFailed(PackageValidationSet validationSet);
        Task OnValidationSetSucceeded(PackageValidationSet validationSet);
        Task OnInProgressValidation(PackageValidationSet validationSet);
    }
}
