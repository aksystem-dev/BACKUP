﻿#pragma checksum "..\..\..\Pages\RestorePage.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "1795F8BFC9F7202992417C70BC2D8467E4F88D72B7450618482D134DB488786F"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using SmartModulBackupClasses;
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
using smart_modul_BACKUP;


namespace smart_modul_BACKUP {
    
    
    /// <summary>
    /// RestorePage
    /// </summary>
    public partial class RestorePage : System.Windows.Controls.Page, System.Windows.Markup.IComponentConnector {
        
        
        #line 72 "..\..\..\Pages\RestorePage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.RadioButton rbt_local;
        
        #line default
        #line hidden
        
        
        #line 78 "..\..\..\Pages\RestorePage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.RadioButton rbt_remote;
        
        #line default
        #line hidden
        
        
        #line 90 "..\..\..\Pages\RestorePage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.StackPanel panel_foldersToRestore;
        
        #line default
        #line hidden
        
        
        #line 144 "..\..\..\Pages\RestorePage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.StackPanel panel_filesToRestore;
        
        #line default
        #line hidden
        
        
        #line 198 "..\..\..\Pages\RestorePage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.StackPanel panel_dbsToRestore;
        
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
            System.Uri resourceLocater = new System.Uri("/smart modul BACKUP;component/pages/restorepage.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\Pages\RestorePage.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal System.Delegate _CreateDelegate(System.Type delegateType, string handler) {
            return System.Delegate.CreateDelegate(delegateType, this, handler);
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
            
            #line 44 "..\..\..\Pages\RestorePage.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.btn_click_back);
            
            #line default
            #line hidden
            return;
            case 2:
            
            #line 50 "..\..\..\Pages\RestorePage.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.btn_click_restore);
            
            #line default
            #line hidden
            return;
            case 3:
            
            #line 53 "..\..\..\Pages\RestorePage.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.btn_click_cancel);
            
            #line default
            #line hidden
            return;
            case 4:
            this.rbt_local = ((System.Windows.Controls.RadioButton)(target));
            return;
            case 5:
            this.rbt_remote = ((System.Windows.Controls.RadioButton)(target));
            return;
            case 6:
            this.panel_foldersToRestore = ((System.Windows.Controls.StackPanel)(target));
            return;
            case 7:
            
            #line 92 "..\..\..\Pages\RestorePage.xaml"
            ((System.Windows.Controls.DataGrid)(target)).SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.dg_cancelSelection);
            
            #line default
            #line hidden
            return;
            case 8:
            this.panel_filesToRestore = ((System.Windows.Controls.StackPanel)(target));
            return;
            case 9:
            
            #line 146 "..\..\..\Pages\RestorePage.xaml"
            ((System.Windows.Controls.DataGrid)(target)).SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.dg_cancelSelection);
            
            #line default
            #line hidden
            return;
            case 10:
            this.panel_dbsToRestore = ((System.Windows.Controls.StackPanel)(target));
            return;
            case 11:
            
            #line 200 "..\..\..\Pages\RestorePage.xaml"
            ((System.Windows.Controls.DataGrid)(target)).SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.dg_cancelSelection);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

