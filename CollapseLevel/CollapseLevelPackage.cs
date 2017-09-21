using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace CollapseLevel
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(CollapseLevelPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class CollapseLevelPackage : Package
    {
        public const string PackageGuidString = "eb99011d-bdb0-42ab-90f9-de4424858b1c";

        public CollapseLevelPackage()
        { }

        protected override void Initialize()
        {
            CollapseLevel.Initialize(this);
            base.Initialize();
        }
    }
}