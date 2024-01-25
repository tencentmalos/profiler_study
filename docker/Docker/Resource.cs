using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Docker;

[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
[DebuggerNonUserCode]
[CompilerGenerated]
internal class Resource
{
	private static ResourceManager resourceMan;

	private static CultureInfo resourceCulture;

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static ResourceManager ResourceManager
	{
		get
		{
			if (resourceMan == null)
			{
				resourceMan = new ResourceManager("Docker.Resource", typeof(Resource).Assembly);
			}
			return resourceMan;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static CultureInfo Culture
	{
		get
		{
			return resourceCulture;
		}
		set
		{
			resourceCulture = value;
		}
	}

	internal static Bitmap CloseButton => (Bitmap)ResourceManager.GetObject("CloseButton", resourceCulture);

	internal static Bitmap CloseButtonHi => (Bitmap)ResourceManager.GetObject("CloseButtonHi", resourceCulture);

	internal static Bitmap DockOptionBottom => (Bitmap)ResourceManager.GetObject("DockOptionBottom", resourceCulture);

	internal static Bitmap DockOptionCentre => (Bitmap)ResourceManager.GetObject("DockOptionCentre", resourceCulture);

	internal static Bitmap DockOptionLeft => (Bitmap)ResourceManager.GetObject("DockOptionLeft", resourceCulture);

	internal static Bitmap DockOptionMDI => (Bitmap)ResourceManager.GetObject("DockOptionMDI", resourceCulture);

	internal static Bitmap DockOptionRight => (Bitmap)ResourceManager.GetObject("DockOptionRight", resourceCulture);

	internal static Bitmap DockOptionTop => (Bitmap)ResourceManager.GetObject("DockOptionTop", resourceCulture);

	internal static Bitmap MaximiseButton => (Bitmap)ResourceManager.GetObject("MaximiseButton", resourceCulture);

	internal static Bitmap MaximiseButtonHi => (Bitmap)ResourceManager.GetObject("MaximiseButtonHi", resourceCulture);

	internal static Bitmap MinimiseButton => (Bitmap)ResourceManager.GetObject("MinimiseButton", resourceCulture);

	internal static Bitmap MinimiseButtonHi => (Bitmap)ResourceManager.GetObject("MinimiseButtonHi", resourceCulture);

	internal static Bitmap RestoreButton => (Bitmap)ResourceManager.GetObject("RestoreButton", resourceCulture);

	internal static Bitmap RestoreButtonHi => (Bitmap)ResourceManager.GetObject("RestoreButtonHi", resourceCulture);

	internal Resource()
	{
	}
}
