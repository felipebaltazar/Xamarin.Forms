using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.CustomAttributes;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Xaml;

#if UITEST
using Xamarin.UITest;
using NUnit.Framework;
using Xamarin.Forms.Core.UITests;
#endif

namespace Xamarin.Forms.Controls.Issues
{
	[Preserve(AllMembers = true)]
	[Issue(IssueTracker.Github, 8285, "Flyout icon not displaying after upgrading to version 4.3 ",
		PlatformAffected.Android | PlatformAffected.iOS)]
#if UITEST
	[NUnit.Framework.Category(UITestCategories.Shell)]
#endif
	public partial class Issue8285 : TestShell
	{
        public Issue8285()
        {
#if APP
			InitializeComponent();
#endif
        }

		protected override void Init()
		{
		}

#if UITEST
		[Test]
		public void NavigatingBackAndForthDoesNotCrash()
		{
			TapInFlyout("Qr Code");

			TapInFlyout("Home");
		}
#endif
	}
}