﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DnsCore {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Errors {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Errors() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("DnsCore.Errors", typeof(Errors).Assembly);
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
        ///   Looks up a localized string similar to Label can contain letters or digits or hyphens.
        /// </summary>
        internal static string Label_CanContainLettersOrDigitsOrHyphen {
            get {
                return ResourceManager.GetString("Label_CanContainLettersOrDigitsOrHyphen", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Label should be at least 1 character length.
        /// </summary>
        internal static string Label_LengthShouldBeAtLeastOne {
            get {
                return ResourceManager.GetString("Label_LengthShouldBeAtLeastOne", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Label should have length less then or equal to {0}.
        /// </summary>
        internal static string Label_LengthShouldNotBeMoreThanMaxFormat {
            get {
                return ResourceManager.GetString("Label_LengthShouldNotBeMoreThanMaxFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Label should end with letter or digit.
        /// </summary>
        internal static string Label_ShouldEndWithLetterOrDigit {
            get {
                return ResourceManager.GetString("Label_ShouldEndWithLetterOrDigit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Label should start with letter.
        /// </summary>
        internal static string Label_ShouldStartWithLetter {
            get {
                return ResourceManager.GetString("Label_ShouldStartWithLetter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Name should have length less then or equal to {0}.
        /// </summary>
        internal static string Name_LengthShouldNotBeMoreThanMaxFormat {
            get {
                return ResourceManager.GetString("Name_LengthShouldNotBeMoreThanMaxFormat", resourceCulture);
            }
        }
    }
}
