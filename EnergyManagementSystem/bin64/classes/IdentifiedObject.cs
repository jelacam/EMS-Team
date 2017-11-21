//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace EMS {
    using System;
    using EMS;
    
    
    /// This is a root class to provide common identification for all classes needing identification and naming attributes.
    public class IdentifiedObject : IDClass {
        
        /// Master resource identifier issued by a model authority. The mRID is globally unique within an exchange context.
        ///Global uniqeness is easily achived by using a UUID for the mRID. It is strongly recommended to do this.
        ///For CIMXML data files in RDF syntax, the mRID is mapped to rdf:ID or rdf:about attributes that identify CIM object elements.
        private string cim_mRID;
        
        private const bool isMRIDMandatory = false;
        
        private const string _mRIDPrefix = "cim";
        
        /// The name is any free human readable and possibly non unique text naming the object.
        private string cim_name;
        
        private const bool isNameMandatory = false;
        
        private const string _namePrefix = "cim";
        
        public virtual string MRID {
            get {
                return this.cim_mRID;
            }
            set {
                this.cim_mRID = value;
            }
        }
        
        public virtual bool MRIDHasValue {
            get {
                return this.cim_mRID != null;
            }
        }
        
        public static bool IsMRIDMandatory {
            get {
                return isMRIDMandatory;
            }
        }
        
        public static string MRIDPrefix {
            get {
                return _mRIDPrefix;
            }
        }
        
        public virtual string Name {
            get {
                return this.cim_name;
            }
            set {
                this.cim_name = value;
            }
        }
        
        public virtual bool NameHasValue {
            get {
                return this.cim_name != null;
            }
        }
        
        public static bool IsNameMandatory {
            get {
                return isNameMandatory;
            }
        }
        
        public static string NamePrefix {
            get {
                return _namePrefix;
            }
        }
    }
}
