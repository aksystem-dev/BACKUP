﻿#pragma checksum "..\..\..\Controls\ToggleOnOff.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "0D0DD4C3D8F68B030EB2DA31B5087B14439B13613FF06E336B177B1C339BCE78"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace smart_modul_BACKUP {
    
    
    /// <summary>
    /// ToggleOnOff
    /// </summary>
    public partial class ToggleOnOff : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 1 "..\..\..\Controls\ToggleOnOff.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal smart_modul_BACKUP.ToggleOnOff root;
        
        #line default
        #line hidden
        
        
        #line 12 "..\..\..\Controls\ToggleOnOff.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Border border;
        
        #line default
        #line hidden
        
        
        #line 31 "..\..\..\Controls\ToggleOnOff.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Border borderInner;
        
        #line default
        #line hidden
        
        
        #line 33 "..\..\..\Controls\ToggleOnOff.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Shapes.Ellipse Toggle;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/smart modul BACKUP;component/controls/toggleonoff.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\Controls\ToggleOnOff.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.root = ((smart_modul_BACKUP.ToggleOnOff)(target));
            
            #line 10 "..\..\..\Controls\ToggleOnOff.xaml"
            this.root.MouseUp += new System.Windows.Input.MouseButtonEventHandler(this.clicked);
            
            #line default
            #line hidden
            return;
            case 2:
            this.border = ((System.Windows.Controls.Border)(target));
            
            #line 12 "..\..\..\Controls\ToggleOnOff.xaml"
            this.border.IsMouseDirectlyOverChanged += new System.Windows.DependencyPropertyChangedEventHandler(this.border_mouseOverChanged);
            
            #line default
            #line hidden
            return;
            case 3:
            this.borderInner = ((System.Windows.Controls.Border)(target));
            return;
            case 4:
            this.Toggle = ((System.Windows.Shapes.Ellipse)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

