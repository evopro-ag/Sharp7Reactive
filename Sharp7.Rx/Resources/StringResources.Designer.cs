﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Sharp7.Rx.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class StringResources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal StringResources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Sharp7.Rx.Resources.StringResources", typeof(StringResources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to S7 driver could not be initialized.
        /// </summary>
        internal static string StrErrorS7DriverCouldNotBeInitialized {
            get {
                return ResourceManager.GetString("StrErrorS7DriverCouldNotBeInitialized", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to S7 driver is not initialized..
        /// </summary>
        internal static string StrErrorS7DriverNotInitialized {
            get {
                return ResourceManager.GetString("StrErrorS7DriverNotInitialized", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to TCP/IP connection established..
        /// </summary>
        internal static string StrInfoConnectionEstablished {
            get {
                return ResourceManager.GetString("StrInfoConnectionEstablished", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Trying to connect to PLC ({2}) &apos;{0}&apos;, CPU slot {1}....
        /// </summary>
        internal static string StrInfoTryConnecting {
            get {
                return ResourceManager.GetString("StrInfoTryConnecting", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error while reading data from plc..
        /// </summary>
        internal static string StrLogErrorReadingDataFromPlc {
            get {
                return ResourceManager.GetString("StrLogErrorReadingDataFromPlc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Communication error discovered. Reconnect is in progress....
        /// </summary>
        internal static string StrLogWarningCommunictionErrorReconnecting {
            get {
                return ResourceManager.GetString("StrLogWarningCommunictionErrorReconnecting", resourceCulture);
            }
        }
    }
}
