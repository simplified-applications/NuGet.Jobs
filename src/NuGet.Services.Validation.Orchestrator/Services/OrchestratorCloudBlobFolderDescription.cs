// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Jobs.Validation;
using NuGetGallery;

namespace NuGet.Services.Validation.Orchestrator
{
    internal class OrchestratorCloudBlobFolderDescription : ValidationCloudBlobFolderDescriptionBase
    {
        public override string GetCacheControl(string folderName)
        {
            switch (folderName)
            {
                case CoreConstants.Folders.FlatContainerFolderName:
                    return null;

                default:
                    return base.GetCacheControl(folderName);
            }
        }

        public override string GetContentType(string folderName)
        {
            switch (folderName)
            {
                case CoreConstants.Folders.FlatContainerFolderName:
                    return CoreConstants.PackageContentType;

                default:
                    return base.GetContentType(folderName);
            }
        }

        public override bool IsPublicContainer(string folderName)
        {
            switch (folderName)
            {
                case CoreConstants.Folders.FlatContainerFolderName:
                    return true;

                default:
                    return base.IsPublicContainer(folderName);
            }
        }
    }
}
