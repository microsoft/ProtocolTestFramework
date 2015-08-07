// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Collections.ObjectModel;

namespace Microsoft.Protocols.ReportingTool
{
    /// <summary>
    /// RT internal use only to dump the requirements.
    /// Collection of requirement which can be serialized.
    /// </summary>
    [Serializable]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/windows/ProtocolsTest/2007/07/RequirementTable")]
    [XmlRootAttribute(Namespace = "http://schemas.microsoft.com/windows/ProtocolsTest/2007/07/RequirementTable", IsNullable = false, ElementName = "ReqTable")]
    public class RequirementCollection
    {
        private List<SerializableRequirement> requirements = new List<SerializableRequirement>();

        /// <summary>
        /// Get all requirement objects.
        /// </summary>
        [XmlElementAttribute("Requirement")]
        public SerializableRequirement[] Requirements
        {
            get
            {
                return this.requirements.ToArray();
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this.requirements.Clear();
                AddRequirements(value);
            }
        }

        /// <summary>
        /// Add requirements to collection
        /// </summary>
        /// <param name="items">The requirement to be added</param>
        public void AddRequirements(SerializableRequirement[] items)
        {
            foreach (SerializableRequirement req in items)
            {
                if (!this.requirements.Contains(req))
                {
                    this.requirements.Add(req);
                }
            }
        }

        /// <summary>
        /// Sort the order of the requirements in collection.
        /// </summary>
        public void Sort()
        {
            this.requirements.Sort();
        }
    }

    /// <summary>
    /// RT internal use only to dump the requirements.
    /// A serializable object to represent a requirement.
    /// </summary>
    [SerializableAttribute()]
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/windows/ProtocolsTest/2007/07/RequirementTable")]
    public class SerializableRequirement : IComparable<SerializableRequirement>
    {
        #region Fields
        //required
        private string req_IDField;

        //optional
        private string doc_SectField;

        private bool doc_SectFieldSpecified;

        //required
        private string descriptionField;

        //optional
        private string posField;

        private bool posFieldSpecified;

        //optional
        private string negField;

        private bool negFieldSpecified;

        //optional
        private string derivedField;

        private bool derivedFieldSpecified;

        //optional
        private string scenarioField;

        private bool scenarioFieldSpecified;

        //optional
        private IsExtensionType isExtensionField;

        private bool isExtensionFieldSpecified;

        //optional
        private BehaviorType behaviorField;

        private bool behaviorFieldSpecified;

        //optional
        private ActorType actorField;

        private bool actorFieldSpecified;

        //optional
        private string scopeField;

        private bool scopeFieldSpecified;

        //optional
        private PriorityType priorityField;

        private bool priorityFieldSpecified;

        //required
        private IsNormativeType isNormativeField;

        //required
        private VerificationType verificationField;

        //optional
        private string verificationCommentField;

        private bool verificationCommentFieldSpecified;

        //additional property
        private IsCoveredType coveredStatus;

        #endregion

        #region Properties
        /// <remarks/>
        public string REQ_ID
        {
            get
            {
                return this.req_IDField;
            }
            set
            {
                this.req_IDField = value;
            }
        }

        /// <remarks/>
        public string Doc_Sect
        {
            get
            {
                return this.doc_SectField;
            }
            set
            {
                this.doc_SectField = value;
            }
        }

        [XmlIgnoreAttribute()]
        public bool Doc_SectSpecified
        {
            get
            {
                return this.doc_SectFieldSpecified;
            }
            set
            {
                this.doc_SectFieldSpecified = value;
            }
        }

        /// <remarks/>
        public string Description
        {
            get
            {
                return this.descriptionField;
            }
            set
            {
                this.descriptionField = value;
            }
        }

        /// <remarks/>
        public string Pos
        {
            get
            {
                return this.posField;
            }
            set
            {
                this.posField = value;
            }
        }

        [XmlIgnoreAttribute()]
        public bool PosSpecified
        {
            get
            {
                return this.posFieldSpecified;
            }
            set
            {
                this.posFieldSpecified = value;
            }
        }

        /// <remarks/>
        public string Neg
        {
            get
            {
                return this.negField;
            }
            set
            {
                this.negField = value;
            }
        }

        [XmlIgnoreAttribute()]
        public bool NegSpecified
        {
            get
            {
                return negFieldSpecified;
            }
            set
            {
                this.negFieldSpecified = value;
            }
        }

        /// <remarks/>
        public string Derived
        {
            get
            {
                return this.derivedField;
            }
            set
            {
                this.derivedField = value;
            }
        }

        [XmlIgnoreAttribute()]
        public bool DerivedSpecified
        {
            get
            {
                return this.derivedFieldSpecified;
            }
            set
            {
                this.derivedFieldSpecified = value;
            }
        }

        /// <remarks/>
        public string Scenario
        {
            get
            {
                return this.scenarioField;
            }
            set
            {
                this.scenarioField = value;
            }
        }

        [XmlIgnoreAttribute()]
        public bool ScenarioSpecified
        {
            get
            {
                return this.scenarioFieldSpecified;
            }
            set
            {
                this.scenarioFieldSpecified = value;
            }
        }

        /// <remarks/>
        public IsExtensionType IsExtension
        {
            get
            {
                return this.isExtensionField;
            }
            set
            {
                this.isExtensionField = value;
            }
        }

        /// <remarks/>
        [XmlIgnoreAttribute()]
        public bool IsExtensionSpecified
        {
            get
            {
                return this.isExtensionFieldSpecified;
            }
            set
            {
                this.isExtensionFieldSpecified = value;
            }
        }

        /// <remarks/>
        public BehaviorType Behavior
        {
            get
            {
                return this.behaviorField;
            }
            set
            {
                this.behaviorField = value;
            }
        }

        /// <remarks/>
        [XmlIgnoreAttribute()]
        public bool BehaviorSpecified
        {
            get
            {
                return this.behaviorFieldSpecified;
            }
            set
            {
                this.behaviorFieldSpecified = value;
            }
        }

        /// <remarks/>
        public ActorType Actor
        {
            get
            {
                return this.actorField;
            }
            set
            {
                this.actorField = value;
            }
        }

        /// <remarks/>
        [XmlIgnoreAttribute()]
        public bool ActorSpecified
        {
            get
            {
                return this.actorFieldSpecified;
            }
            set
            {
                this.actorFieldSpecified = value;
            }
        }

        /// <remarks/>
        public string Scope
        {
            get
            {
                return this.scopeField;
            }
            set
            {
                this.scopeField = value;
            }
        }

        [XmlIgnoreAttribute()]
        public bool ScopeSpecified
        {
            get
            {
                return this.scopeFieldSpecified;
            }
            set
            {
                this.scopeFieldSpecified = value;
            }
        }

        /// <remarks/>
        public PriorityType Priority
        {
            get
            {
                return this.priorityField;
            }
            set
            {
                this.priorityField = value;
            }
        }

        /// <remarks/>
        [XmlIgnoreAttribute()]
        public bool PrioritySpecified
        {
            get
            {
                return this.priorityFieldSpecified;
            }
            set
            {
                this.priorityFieldSpecified = value;
            }
        }

        /// <remarks/>
        public IsNormativeType IsNormative
        {
            get
            {
                return this.isNormativeField;
            }
            set
            {
                this.isNormativeField = value;
            }
        }

        /// <remarks/>
        public VerificationType Verification
        {
            get
            {
                return this.verificationField;
            }
            set
            {
                this.verificationField = value;
            }
        }

        /// <remarks/>
        public string VerificationComment
        {
            get
            {
                return this.verificationCommentField;
            }
            set
            {
                this.verificationCommentField = value;
            }
        }

        [XmlIgnoreAttribute()]
        public bool VerificationCommentSpecified
        {
            get
            {
                return this.verificationCommentFieldSpecified;
            }
            set
            {
                this.verificationCommentFieldSpecified = value;
            }
        }

        /// <remarks/>
        public IsCoveredType CoveredStatus
        {
            get
            {
                return this.coveredStatus;
            }
            set
            {
                this.coveredStatus = value;
            }
        }

        #endregion

        #region IComparable<SerializableRequirement> Members

        public int CompareTo(SerializableRequirement other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            int ret = 0;
            //try get the number from Id to compare.
            Regex regex = new Regex(@"\d+");
            if (regex.IsMatch(this.req_IDField) &&
                regex.IsMatch(other.req_IDField))
            {
                Match thisMatch = regex.Match(this.req_IDField);
                Match otherMatch = regex.Match(other.req_IDField);
                ulong thisId = ulong.Parse(thisMatch.Value);
                ulong otherId = ulong.Parse(otherMatch.Value);
                ret = thisId.CompareTo(otherId);
            }

            //if no number or the same numbers.
            if (ret == 0)
            {
                ret = this.req_IDField.CompareTo(other.req_IDField);
            }

            return ret;
        }

        #endregion
    }

    /// <summary>
    /// Extension type
    /// </summary>
    [SerializableAttribute()]
    [XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/windows/ProtocolsTest/2007/07/RequirementTable")]
    public enum IsExtensionType
    {
        /// <summary>
        /// Non-extension type
        /// </summary>
        [XmlEnumAttribute("Non-extension")]
        Nonextension,

        /// <summary>
        /// Extension type
        /// </summary>
        Extension,
    }

    /// <summary>
    /// Behavior type
    /// </summary>
    [SerializableAttribute()]
    [XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/windows/ProtocolsTest/2007/07/RequirementTable")]
    public enum BehaviorType
    {
        /// <summary>
        /// Protocol behavior
        /// </summary>
        Protocol,

        /// <summary>
        /// Windows behavior
        /// </summary>
        Windows,

        /// <summary>
        /// Product behavior
        /// </summary>
        Product,

        /// <summary>
        /// Microsoft product specific
        /// </summary>
        Microsoft,
    }

    /// <summary>
    /// Actor Type
    /// </summary>
    [SerializableAttribute()]
    [XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/windows/ProtocolsTest/2007/07/RequirementTable")]
    public enum ActorType
    {
        /// <summary>
        /// Act as client side
        /// </summary>
        Client,

        /// <summary>
        /// Act as server side
        /// </summary>
        Server,

        /// <summary>
        /// Act as both client and server sides
        /// </summary>
        Both,
    }

    /// <summary>
    /// Requirement priority type
    /// </summary>
    [SerializableAttribute()]
    [XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/windows/ProtocolsTest/2007/07/RequirementTable")]
    public enum PriorityType
    {
        /// <summary>
        /// P0 level requirement
        /// </summary>
        p0,

        /// <summary>
        /// P1 level requirement
        /// </summary>
        p1,

        /// <summary>
        /// P2 level requirement
        /// </summary>
        p2,
    }

    /// <summary>
    /// Normative/Informative type
    /// </summary>
    [SerializableAttribute()]
    [XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/windows/ProtocolsTest/2007/07/RequirementTable")]
    public enum IsNormativeType
    {
        /// <summary>
        /// The normative type
        /// </summary>
        Normative,

        /// <summary>
        /// The informative type
        /// </summary>
        Informative,
    }

    /// <summary>
    /// Requirement verification type
    /// </summary>
    [SerializableAttribute()]
    [XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/windows/ProtocolsTest/2007/07/RequirementTable")]
    public enum VerificationType
    {
        /// <summary>
        /// Non-testable requirement
        /// </summary>
        [XmlEnumAttribute("Non-testable")]
        Nontestable,

        /// <summary>
        /// Adapter requirement
        /// </summary>
        Adapter,

        /// <summary>
        /// Test Case requirement
        /// </summary>
        [XmlEnumAttribute("Test Case")]
        TestCase,

        /// <summary>
        /// Unverified requirement
        /// </summary>
        Unverified,

        /// <summary>
        /// Deleted requirement
        /// </summary>
        Deleted,
    }

    /// <summary>
    /// Requirement covered status
    /// </summary>
    [SerializableAttribute()]
    [XmlTypeAttribute(Namespace = "http://schemas.microsoft.com/windows/ProtocolsTest/2007/07/RequirementTable")]
    public enum IsCoveredType
    {
        /// <summary>
        /// The requirement is not covered
        /// </summary>
        [XmlEnumAttribute("Not Covered")]
        NotCovered,

        /// <summary>
        /// The requirement is covered
        /// </summary>
        Covered,
    }
}
