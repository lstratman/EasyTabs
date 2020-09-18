# EasyTabs

[![Build status](https://ci.appveyor.com/api/projects/status/w3v4642dkec8c8jw?svg=true)](https://ci.appveyor.com/project/lstratman/easytabs-815)

<a href="http://lstratman.github.com/EasyTabs/images/screenshots/tabs.png" target="_blank"><img src="http://lstratman.github.com/EasyTabs/images/screenshots/thumbnails/tabs.png"/></a>
<a href="http://lstratman.github.com/EasyTabs/images/screenshots/repositioning.png" target="_blank"><img src="http://lstratman.github.com/EasyTabs/images/screenshots/thumbnails/repositioning.png"/></a>
<a href="http://lstratman.github.com/EasyTabs/images/screenshots/tearing.png" target="_blank"><img src="http://lstratman.github.com/EasyTabs/images/screenshots/thumbnails/tearing.png"/></a>
<a href="http://lstratman.github.com/EasyTabs/images/screenshots/peek.png" target="_blank"><img src="http://lstratman.github.com/EasyTabs/images/screenshots/thumbnails/peek.png"/></a>

EasyTabs is a library that you can add to your .NET WinForms applications (WPF support coming eventually) in order to render a list of tabs in the title bar of the application, similar to Chrome, Firefox, Edge, etc.  Instead of inheriting from `System.Windows.Forms.Form`, you inherit from `EasyTabs.TitleBarTabs` and set the tab renderer that you wish to use:

```cs
using EasyTabs;

namespace YourNamespace
{
    public partial class YourApp : TitleBarTabs
    {
        public TestApp()
        {
            InitializeComponent();

            // Enable or disable viewing tabs through the taskbar
            AeroPeekEnabled = true;
            // Set the tab rendering engine that you wish to use
            TabRenderer = new ChromeTabRenderer(this);
        }
        
        // ...
    }
}
```

The base class takes care of the grunt work of rendering the tabs, responding to clicks to activate/close/add/etc. while you simply add `TitleBarTab` objects to the `Tabs` collection.  `TitleBarTab` objects expect their `Content` property to be set to a `Form` object that represents the contents for the tab.  You can design these forms in Visual Studio the same as you would any other application; the `Title` and `Icon` properties are used to display the tab itself.

The library comes with a renderer for Chrome-like tabs (`ChromeTabRenderer`), but you can implement your own by creating a class inheriting from `BaseTabRenderer`.  The TestApp directory contains the project for a tabbed web browser test application that you can use as a starting point for implementing your own functionality.

You can include this functionality in your project via NuGet:

    PM> Install-Package EasyTabs

This project is licensed under the terms of the [BSD license](BSD.txt)