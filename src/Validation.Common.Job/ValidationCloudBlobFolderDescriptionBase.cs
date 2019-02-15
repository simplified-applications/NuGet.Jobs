// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using NuGetGallery;

namespace NuGet.Jobs.Validation
{
    public class ValidationCloudBlobFolderDescriptionBase : ICloudBlobFolderDescription
    {
        public virtual string GetCacheControl(string folderName)
        {
            switch (folderName)
            {
                case CoreConstants.Folders.PackageBackupsFolderName:
                case CoreConstants.Folders.ValidationFolderName:
                    return null;

                case CoreConstants.Folders.PackagesFolderName:
                    return CoreConstants.DefaultCacheControl;

                default:
                    throw new InvalidOperationException($"The folder name '{folderName}' is not supported");
            }
        }

        public virtual string GetContentType(string folderName)
        {
            switch (folderName)
            {
                case CoreConstants.Folders.ValidationFolderName:
                case CoreConstants.Folders.PackageBackupsFolderName:
                case CoreConstants.Folders.PackagesFolderName:
                    return CoreConstants.PackageContentType;

                default:
                    throw new InvalidOperationException($"The folder name '{folderName}' is not supported");
            }
        }

        public virtual bool IsPublicContainer(string folderName)
        {
            switch (folderName)
            {
                case CoreConstants.Folders.PackageBackupsFolderName:
                case CoreConstants.Folders.PackagesFolderName:
                    return true;

                case CoreConstants.Folders.ValidationFolderName:
                    return false;

                default:
                    throw new InvalidOperationException($"The folder name '{folderName}' is not supported");
            }
        }
    }
}
