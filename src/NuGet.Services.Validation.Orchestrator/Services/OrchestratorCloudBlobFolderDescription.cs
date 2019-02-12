// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGetGallery;
using System;

namespace NuGet.Services.Validation.Orchestrator
{
    internal class OrchestratorCloudBlobFolderDescription : ICloudBlobFolderDescription
    {
        public string GetCacheControl(string folderName)
        {
            switch (folderName)
            {
                case CoreConstants.Folders.FlatContainerFolderName:
                    return null;

                default:
                    throw new InvalidOperationException($"The folder name '{folderName}' is not supported");
            }
        }

        public string GetContentType(string folderName)
        {
            switch (folderName)
            {
                case CoreConstants.Folders.FlatContainerFolderName:
                    return CoreConstants.PackageContentType;

                default:
                    throw new InvalidOperationException($"The folder name '{folderName}' is not supported");
            }
        }

        public bool IsPublicContainer(string folderName)
        {
            switch (folderName)
            {
                case CoreConstants.Folders.FlatContainerFolderName:
                    return true;

                default:
                    throw new InvalidOperationException($"The folder name '{folderName}' is not supported");
            }
        }
    }
}
