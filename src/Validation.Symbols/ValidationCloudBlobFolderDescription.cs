// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Jobs.Validation;
using NuGetGallery;

namespace Validation.Symbols
{
    internal class ValidationCloudBlobFolderDescription : ValidationCloudBlobFolderDescriptionBase
    {
        public override string GetCacheControl(string folderName)
        {
            switch (folderName)
            {
                case CoreConstants.Folders.SymbolPackagesFolderName:
                    return CoreConstants.DefaultCacheControl;

                default:
                    return base.GetCacheControl(folderName);
            }
        }

        public override string GetContentType(string folderName)
        {
            switch (folderName)
            {
                case CoreConstants.Folders.SymbolPackagesFolderName:
                    return CoreConstants.PackageContentType;

                default:
                    return base.GetContentType(folderName);
            }
        }

        public override bool IsPublicContainer(string folderName)
        {
            switch (folderName)
            {
                case CoreConstants.Folders.SymbolPackagesFolderName:
                    return true;

                default:
                    return base.IsPublicContainer(folderName);
            }
        }
    }
}
