// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace NuGet.Services.Validation.Orchestrator
{
    public class ValidationRequest : IValidationRequest
    {
        public Guid ValidationId { get; }

        public IValidatable Validatable { get; }

        public string BlobUrl { get; }

        public ValidationRequest(Guid validationId, IValidatable validatable, string blobUrl)
        {
            ValidationId = validationId;
            Validatable = validatable;
            BlobUrl = blobUrl;
        }
    }
}
