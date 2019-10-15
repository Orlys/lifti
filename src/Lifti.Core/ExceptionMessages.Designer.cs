﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Lifti {
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
    internal class ExceptionMessages {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal ExceptionMessages() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Lifti.ExceptionMessages", typeof(ExceptionMessages).Assembly);
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
        ///   Looks up a localized string similar to Expected a text token - got {0}.
        /// </summary>
        internal static string ExpectedTextToken {
            get {
                return ResourceManager.GetString("ExpectedTextToken", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Token expected: {0}.
        /// </summary>
        internal static string ExpectedToken {
            get {
                return ResourceManager.GetString("ExpectedToken", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Field id {0} has no associated field name.
        /// </summary>
        internal static string FieldHasNoAssociatedFieldName {
            get {
                return ResourceManager.GetString("FieldHasNoAssociatedFieldName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Id {0} is already registered in the index..
        /// </summary>
        internal static string IdAlreadyUsed {
            get {
                return ResourceManager.GetString("IdAlreadyUsed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An index must be empty when attempting to deserialize its contents..
        /// </summary>
        internal static string IndexMustBeEmptyForDeserialization {
            get {
                return ResourceManager.GetString("IndexMustBeEmptyForDeserialization", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Item already indexed.
        /// </summary>
        internal static string ItemAlreadyIndexed {
            get {
                return ResourceManager.GetString("ItemAlreadyIndexed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Item not found.
        /// </summary>
        internal static string ItemNotFound {
            get {
                return ResourceManager.GetString("ItemNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Only 255 distinct fields can currently be indexed.
        /// </summary>
        internal static string MaximumDistinctFieldsIndexReached {
            get {
                return ResourceManager.GetString("MaximumDistinctFieldsIndexReached", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The index terminator was not found at the end of the file..
        /// </summary>
        internal static string MissingIndexTerminator {
            get {
                return ResourceManager.GetString("MissingIndexTerminator", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The expected header bytes could not be found in the stream - this is probably not a serialized index..
        /// </summary>
        internal static string MissingLiftiHeaderIndicatorBytes {
            get {
                return ResourceManager.GetString("MissingLiftiHeaderIndicatorBytes", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No deserializer available for binary serialized version {0}..
        /// </summary>
        internal static string NoDeserializerAvailableForIndexVersion {
            get {
                return ResourceManager.GetString("NoDeserializerAvailableForIndexVersion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to read header data from serialized index content..
        /// </summary>
        internal static string UnableToReadHeaderInformation {
            get {
                return ResourceManager.GetString("UnableToReadHeaderInformation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The query ended unexpectedly - a token was expected..
        /// </summary>
        internal static string UnexpectedEndOfQuery {
            get {
                return ResourceManager.GetString("UnexpectedEndOfQuery", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An unexpected operator was encountered: {0}.
        /// </summary>
        internal static string UnexpectedOperator {
            get {
                return ResourceManager.GetString("UnexpectedOperator", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Internal error - An unexpected operator was encountered: {0}.
        /// </summary>
        internal static string UnexpectedOperatorInternal {
            get {
                return ResourceManager.GetString("UnexpectedOperatorInternal", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unexpected token encountered: {0}.
        /// </summary>
        internal static string UnexpectedTokenEncountered {
            get {
                return ResourceManager.GetString("UnexpectedTokenEncountered", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unknown field &apos;{0}&apos; referenced in query.
        /// </summary>
        internal static string UnknownFieldReference {
            get {
                return ResourceManager.GetString("UnknownFieldReference", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unknown operator encountered: {0}.
        /// </summary>
        internal static string UnknownOperatorEncountered {
            get {
                return ResourceManager.GetString("UnknownOperatorEncountered", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unsupported index support level {0}.
        /// </summary>
        internal static string UnsupportedIndexSupportLevel {
            get {
                return ResourceManager.GetString("UnsupportedIndexSupportLevel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unsupported tokenizer kind {0}.
        /// </summary>
        internal static string UnsupportedTokenizerKind {
            get {
                return ResourceManager.GetString("UnsupportedTokenizerKind", resourceCulture);
            }
        }
    }
}
