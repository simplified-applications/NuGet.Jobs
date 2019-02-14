// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using NuGetGallery;

namespace NuGet.Services.Validation.Orchestrator
{
    internal class OrchestratorCloudBlobFolderDescription : ICloudBlobFolderDescription
    {
        public string GetCacheControl(string folderName)
        {
            switch (folderName)
            {
                case CoreConstants.Folders.FlatContainerFolderName:
                case CoreConstants.Folders.PackageBackupsFolderName:
                case CoreConstants.Folders.ValidationFolderName:
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
                case CoreConstants.Folders.ValidationFolderName:
                case CoreConstants.Folders.PackageBackupsFolderName:
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
                case CoreConstants.Folders.PackageBackupsFolderName:
                    return true;

                case CoreConstants.Folders.ValidationFolderName:
                    return false;

                default:
                    throw new InvalidOperationException($"The folder name '{folderName}' is not supported");
            }
        }
    }
}
