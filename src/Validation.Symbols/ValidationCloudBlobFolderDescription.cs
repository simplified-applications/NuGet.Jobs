// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using NuGetGallery;

namespace Validation.Symbols
{
    internal class ValidationCloudBlobFolderDescription : ICloudBlobFolderDescription
    {
        public string GetCacheControl(string folderName)
        {
            switch (folderName)
            {
                case CoreConstants.Folders.PackagesFolderName:
                case CoreConstants.Folders.SymbolPackagesFolderName:
                    return CoreConstants.DefaultCacheControl;

                default:
                    throw new InvalidOperationException($"The folder name '{folderName}' is not supported");
            }
        }

        public string GetContentType(string folderName)
        {
            switch (folderName)
            {
                case CoreConstants.Folders.PackagesFolderName:
                case CoreConstants.Folders.SymbolPackagesFolderName:
                    return CoreConstants.PackageContentType;

                default:
                    throw new InvalidOperationException($"The folder name '{folderName}' is not supported");
            }
        }

        public bool IsPublicContainer(string folderName)
        {
            switch (folderName)
            {
                case CoreConstants.Folders.PackagesFolderName:
                case CoreConstants.Folders.SymbolPackagesFolderName:
                    return true;

                default:
                    throw new InvalidOperationException($"The folder name '{folderName}' is not supported");
            }
        }
    }
}
