// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace Microsoft.Protocols.ReportingTool
{
    internal enum VisitStatus
    {
        Unvisited = 0,
        Visited,
        Removed
    }

    internal class VerificationValues
    {
        public const string NONTESTABLE = "Non-testable";
        public const string ADAPTER = "Adapter";
        public const string TESTCASE = "Test Case";
        public const string UNVERIFIED = "Unverified";
        public const string DELETED = "Deleted";
        public const string NONEXIST = "Non Exist";
    }

    internal class RSValidationRules
    {
        public const string OUTOFSCOPEISTESTABLE = "Requirement is Out-of-Scope but marked as adapter or test case.";
        public const string DERIVEFROMDELETED = "Cannot derive from a Deleted requirement.";
        public const string DERIVEFROMINFORMATIVE = "A requirement cannot be derived from informative requirement alone.";
        public const string DERIVEFROMNONEXIST = "Cannot derive from a non-existent requirement {0}.";
        public const string DERIVEFROMUNVERIFIED = "Cannot derive from a Normative Unverified requirement.";
        public const string DERIVEDREQISINFORMATIVE = "Derived requirement cannot be Informative.";
        public const string DERIVEDREQNOTTESTABLE = "Derived requirement can only be Test Case or Adapter.";
        public const string DERIVEDREQOUTOFSCOPE = "Derived requirement cannot be out-of-scope.";
    }

    internal class TableAnalyzer
    {
        
        private Dictionary<string, List<string>> requirementsToVerify;
        private Dictionary<string, List<string>> requirementsNotToVerify;
        private Dictionary<string, List<string>> requirementsDeleted = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> scopeRules = new Dictionary<string, List<string>>();
        private Dictionary<string, DerivedRequirement> derivedRequirements
            = new Dictionary<string, DerivedRequirement>();
        private List<string> nontestableRequirements = new List<string>();
        bool scopeMode;
        string prefix;
        private Dictionary<string, VisitStatus> reqIDVisitStatus;
        private bool startReqIDTag;
        private Dictionary<string, string> reqVerifications = new Dictionary<string, string>();

        //store requirement spec validation errors informations.
        private Dictionary<string, string> validationErrors = new Dictionary<string, string>();

        //store requirement spec validation warnings informations
        private Dictionary<string, string> validationWarnings = new Dictionary<string, string>();

        //internal use only, to cache all informative requirement, for derive requirement checking
        private List<string> informativeReqs = new List<string>();

        // a list of derived requirement id which derived from informative requirement.
        private List<string> derivedFromInformativeReqs = new List<string>();

        private List<string> deltaScopes = new List<string>();

        private List<string> oriReqOutOfDeltaScope = new List<string>();

        protected TableAnalyzer(
            Dictionary<string, List<string>> scopeRules,
            List<string> deltaScopes,
            bool scopeMode, string prefix)
        {

            requirementsToVerify = new Dictionary<string, List<string>>();


            requirementsNotToVerify = new Dictionary<string, List<string>>();
            if (scopeRules.Count == 0)
            {
                throw new InvalidOperationException(
                    "No scope value is found to compute the requirement verifiable.");
            }
            this.scopeRules = scopeRules;
            this.scopeMode = scopeMode;
            this.prefix = prefix;
            this.deltaScopes = deltaScopes;
            reqIDVisitStatus = new Dictionary<string, VisitStatus>();
        }

        public TableAnalyzer(
            string reqFilename,
            Dictionary<string, List<string>> scopeRules,
            List<string> deltaScopes,
            bool scopeMode, string prefix)
            : this(scopeRules, deltaScopes, scopeMode, prefix)
        {
            IList<string> filenames = new List<string>();
            filenames.Add(reqFilename);
            Load(filenames);
        }
        public TableAnalyzer(
            StringCollection reqFilenames,
            Dictionary<string, List<string>> scopeRules,
            List<string> deltaScopes,
            bool scopeMode, string prefix)
            : this(scopeRules, deltaScopes, scopeMode, prefix)
        {
            IList<string> filenames = new List<string>();
            foreach (string filename in reqFilenames)
            {
                filenames.Add(filename);
            }
            Load(filenames);
        }

        public TableAnalyzer(
            IList<string> reqFilenames,
            Dictionary<string, List<string>> scopeRules,
            List<string> deltaScopes,
            bool scopeMode, string prefix)
            : this(scopeRules, deltaScopes, scopeMode, prefix)
        {
            Load(reqFilenames);
        }

        private static void LogTraceWarning(string message)
        {
            if (ReportingLog.Log != null)
            {
                ReportingLog.Log.TraceWarning(message);
            }
        }
        /// <summary>
        /// compute requirement using old rule
        /// </summary>
        /// <param name="row">The requirement row contains actor column</param>
        private void ComputRequirementV1(ReqTable.RequirementRow row)
        {
            List<string> ltStrings = new List<string>();
            if (row.IsActorNull() && !row.IsScopeNull())
            {
                throw new InvalidOperationException("Using Actor and Scope together is not supported.");
            }
            if (scopeMode)
            {
                throw new InvalidOperationException("Scope is not supported in old version of RS");
            }
            if (deltaScopes.Count > 0)
            {
                throw new InvalidOperationException("Delta scope is not supported in old version of RS");
            }
            if (
                        (String.Compare(row.IsNormative, "Normative", false) == 0)
                        && (String.Compare(row.Actor, "Client", false) != 0)
                        && (String.Compare(row.Verification, "Non-testable", false) != 0)
                        )
            {
                // all requirements that are normative & non-client & testable
                ltStrings.Add(row.Description);
                ltStrings.Add(row.Doc_Sect);
                ltStrings.Add(row.Scope);
                requirementsToVerify.Add(row.REQ_ID, ltStrings);
            }
            else
            {
                ltStrings.Add(row.Description);
                ltStrings.Add(row.Doc_Sect);
                ltStrings.Add(row.Scope);
               
                requirementsNotToVerify.Add(row.REQ_ID, ltStrings);

            }
        }

        /// <summary>
        /// compute requirement using new rule
        /// </summary>
        /// <param name="row">The requirement row contians scope column</param>
        private void ComputRequirementV2(ReqTable.RequirementRow row)
        {
            List<string> ltStrings = new List<string>();
            if (row.IsScopeNull() && !row.IsActorNull())
            {
                throw new InvalidOperationException("Using Actor and Scope together is not supported.");
            }

            row.Scope = row.Scope.Trim();
            string keyword = "none";
            if (string.Compare(row.Scope, keyword, true) == 0)
            {
                throw new InvalidOperationException(
                    string.Format("'None' keyword should not be the value of scope in requirement {0}", row.REQ_ID));
            }

            if (scopeRules[ReportingParameters.inScopeRule].Contains(row.Scope.ToLower()) &&
                    scopeRules[ReportingParameters.outScopeRule].Contains(row.Scope.ToLower()))
            {
                throw new InvalidOperationException(
                    string.Format("Value {0} for InScope and OutOfScope parameters is duplicated.", row.Scope));
            }
            else if (!scopeRules[ReportingParameters.inScopeRule].Contains(row.Scope.ToLower()) &&
                    !scopeRules[ReportingParameters.outScopeRule].Contains(row.Scope.ToLower()))
            {
                throw new InvalidOperationException(
                    string.Format("Unexpected scope value in Requirement {0}.", row.REQ_ID));
            }
            else if (scopeRules[ReportingParameters.inScopeRule].Contains(row.Scope.ToLower()))
            {
                //in-scope requirement
                if (string.Compare("Normative", row.IsNormative, true) == 0)
                {
                    switch (row.Verification)
                    {
                        case VerificationValues.ADAPTER:
                        case VerificationValues.TESTCASE:
                        case VerificationValues.UNVERIFIED:
                            {
                                    if ((deltaScopes.Count > 0 && !deltaScopes.Contains(row.Delta)))
                                {
                                    if (row.IsDerivedNull() || string.IsNullOrEmpty(row.Derived))
                                    {
                                        //original requirement is out of delta scope
                                        ltStrings.Add(row.Description);
                                        ltStrings.Add(row.Doc_Sect);
                                        ltStrings.Add(row.Scope);
                                        requirementsNotToVerify.Add(row.REQ_ID, ltStrings);
                                        oriReqOutOfDeltaScope.Add(row.REQ_ID);
                                    }
                                    else
                                    {
                                        //in-scope, normative, testable requirement
                                        ltStrings.Add(row.Description);
                                        ltStrings.Add(row.Doc_Sect);
                                        ltStrings.Add(row.Scope);
                                        requirementsToVerify.Add(row.REQ_ID, ltStrings);
                                    }
                                }
                                else
                                {
                                    ltStrings.Add(row.Description);
                                    ltStrings.Add(row.Doc_Sect);
                                    ltStrings.Add(row.Scope);
                                    requirementsToVerify.Add(row.REQ_ID, ltStrings);
                                }
                                break;
                            }
                        default:
                            {
                                //in-scope, normative, un-testable requirement
                                ltStrings.Add(row.Description);
                                ltStrings.Add(row.Doc_Sect);
                                ltStrings.Add(row.Scope);
                                requirementsNotToVerify.Add(row.REQ_ID, ltStrings);
                                break;
                            }
                    }
                }
                else
                {
                    //informative requirement should be ignore.
                    ltStrings.Add(row.Description);
                    ltStrings.Add(row.Doc_Sect);
                    ltStrings.Add(row.Scope);
                    requirementsNotToVerify.Add(row.REQ_ID, ltStrings);
                    informativeReqs.Add(row.REQ_ID);
                }
            }
            else
            {
                if (string.Compare("Informative", row.IsNormative, true) == 0)
                {
                    informativeReqs.Add(row.REQ_ID);
                }
                ltStrings.Add(row.Description);
                ltStrings.Add(row.Doc_Sect);
                ltStrings.Add(row.Scope);
                //out of scope requirement should not to verify
                requirementsNotToVerify.Add(row.REQ_ID, ltStrings);
                if (string.Compare(VerificationValues.ADAPTER, row.Verification, true) == 0 ||
                    string.Compare(VerificationValues.TESTCASE, row.Verification, true) == 0)
                {
                    //out of scope requirement should not mark as testable.
                    if (!validationErrors.ContainsKey(row.REQ_ID))
                    {
                        validationErrors.Add(row.REQ_ID, RSValidationRules.OUTOFSCOPEISTESTABLE);
                    }
                }
            }
        }

        private void ComputeDerivedRequirement(ReqTable.RequirementRow row)
        {
            if (row.IsDerivedNull() || String.Compare(row.Verification, VerificationValues.DELETED, true) == 0)
            {
                return;
            }
            string derivedReqs = row.Derived;
            if (!string.IsNullOrEmpty(derivedReqs.Trim()))
            {
                string[] originalReqs = derivedReqs.Split(new char[] { ',' });
                if (originalReqs != null)
                {
                    foreach (string originalReq in originalReqs)
                    {
                        if (string.IsNullOrEmpty(originalReq.Trim()))
                        {
                            continue;
                        }
                        string[] terms = originalReq.Split(new char[] { ':' });
                        if (terms == null || terms.Length > 2)
                        {
                            throw new FormatException(
                                string.Format("The format of derived text {0} in Requirement {1}:\"{2}\" is not correct,"
                                + "the correct format should be Req_ID plus :i :p or :c as suffix.",
                                originalReq, row.REQ_ID, row.Description));
                        }
                        string originalId = terms[0].Trim();
                        if (string.IsNullOrEmpty(originalId))
                        {
                            throw new FormatException(
                                string.Format("The originalID cannot be null or empty in Requirement {1}:\"{2}\"",
                                row.REQ_ID, row.Description));
                        }

                        //check the original requirement exists.
                        originalId = GetRequirementId(originalId, true);
                        if (string.IsNullOrEmpty(originalId))
                        {
                            if (!validationErrors.ContainsKey(row.REQ_ID))
                            {
                                validationErrors.Add(row.REQ_ID,
                                    string.Format(RSValidationRules.DERIVEFROMNONEXIST, terms[0]));
                            }
                            continue;
                        }

                        if (terms.Length == 2)
                        {
                            //treat "R1: " as default
                            if (string.IsNullOrEmpty(terms[1].Trim()))
                            {
                                AddDerivedRequirement("i", originalId, row);
                            }
                            else
                            {
                                AddDerivedRequirement(terms[1].Trim(), originalId, row);
                            }
                        }
                        else
                        {
                            //default type is :i
                            AddDerivedRequirement("i", originalId, row);
                        }
                    }
                }
            }
        }

        private void AddDerived(string originalId, ReqTable.RequirementRow row, DerivedType type)
        {
            if (!derivedRequirements.ContainsKey(originalId))
            {
                DerivedRequirement originalReq
                    = new DerivedRequirement(originalId, CoveredStatus.Unverified);
                originalReq.AddDerivedRequirement(row.REQ_ID, type);
                derivedRequirements.Add(originalId, originalReq);
            }
            else
            {
                if (!derivedRequirements[originalId].DerivedReqs.ContainsKey(row.REQ_ID))
                {
                    DerivedRequirement originalReq = derivedRequirements[originalId];
                    originalReq.AddDerivedRequirement(row.REQ_ID, type);
                    derivedRequirements[originalId] = originalReq;
                }
            }
        }

        //build the graph of derived requirements.
        private void AddDerivedRequirement(string type, string originalId, ReqTable.RequirementRow row)
        {
            if (!ValidateOriginalRequirement(row, originalId))
            {
                //error occurs, should not be added to build derived graph.
                return;
            }
            //add original requirement id to the current requirement's original id list.
            if (!derivedRequirements.ContainsKey(row.REQ_ID))
            {
                DerivedRequirement derivedReq
                    = new DerivedRequirement(row.REQ_ID, CoveredStatus.Unverified);
                derivedReq.AddOriginalRequirement(originalId);
                derivedRequirements.Add(row.REQ_ID, derivedReq);
            }
            else
            {
                if (!derivedRequirements[row.REQ_ID].OriginalReqs.Contains(originalId))
                {
                    DerivedRequirement derivedReq = derivedRequirements[row.REQ_ID];
                    derivedReq.AddOriginalRequirement(originalId);
                    derivedRequirements[row.REQ_ID] = derivedReq;
                }
            }

            //add all derived requirements for current requirement 
            //to the derived dictionary of original requirement
            switch (type.ToLower())
            {
                case "i":
                    AddDerived(originalId, row, DerivedType.Inferred);
                    break;
                case "p":
                    AddDerived(originalId, row, DerivedType.Partial);
                    break;
                case "c":
                    AddDerived(originalId, row, DerivedType.Cases);
                    break;
                default:
                    throw new FormatException(
                        string.Format("Unexpected derived type \"{0}\" is found in derived text in Requirement {1}:\"{2}\"," +
                        "the type must be one of i, c or p.", type, row.REQ_ID, row.Description));
            }
        }

        private void Load(IList<string> reqFilenames)
        {
            try
            {
                ReqTable requirementTable = null;
                ReqTable tempTable = new ReqTable();
                foreach (string reqfile in reqFilenames)
                {
                    if (requirementTable == null)
                    {
                        requirementTable = new ReqTable();
                        requirementTable.ReadXml(XmlReader.Create(reqfile, new XmlReaderSettings() { XmlResolver = null }));
                    }
                    else
                    {
                        tempTable.ReadXml(XmlReader.Create(reqfile, new XmlReaderSettings() { XmlResolver = null }));
                        requirementTable.Merge(tempTable);
                        tempTable.Clear();
                    }
                }
                tempTable.Dispose();

                bool newVersion = false;
                // Now requirementTable contains all requirement tables from user's input
                // to simplify the coding, ReportingTool assumes all of them are user's required.
                foreach (ReqTable.RequirementRow row in requirementTable.Requirement)
                {
                    //remove all start or end spacese.
                    row.REQ_ID = row.REQ_ID.Trim();
                    row.Verification = row.Verification.Trim();

                    //translate the blank to new for delta value
                    if (row.IsDeltaNull() || string.IsNullOrEmpty(row.Delta.Trim()))
                    {
                        row.Delta = "new";
                    }
                    else
                    {
                        row.Delta = row.Delta.ToLower().Trim();
                    }

                    //cache all requirements and verifications.
                    this.reqVerifications.Add(row.REQ_ID, row.Verification);

                    if (string.Compare(row.Verification, VerificationValues.DELETED, true) == 0)
                    {
                        List<string> ltStrings = new List<string>();
                        ltStrings.Add(row.Description);
                        ltStrings.Add(row.Doc_Sect);
                        ltStrings.Add(row.Scope);
                        requirementsDeleted.Add(row.REQ_ID, ltStrings);
                        continue;
                    }

                    TotalCount++;
                    if (row.IsActorNull() && row.IsScopeNull())
                    {
                        throw new InvalidOperationException(
                            String.Format("Column Actor or Scope is expected at {0}", row.REQ_ID));
                    }

                    if (!row.IsScopeNull() && TotalCount == 1)
                    {
                        //estimate RS version from the first requirement
                        newVersion = true;
                    }

                    if (newVersion)
                    {
                        ComputRequirementV2(row);
                    }
                    else
                    {
                        ComputRequirementV1(row);
                    }
                }

                //make sure there is no duplicated id in RS Req_Id column.
                MakeSureNoDuplicateId();

                if (newVersion)
                {
                    //compute derived requirements and original requirements
                    foreach (ReqTable.RequirementRow row in requirementTable.Requirement)
                    {
                        if (ValidateDerivedRequirement(row))
                        {
                            ComputeDerivedRequirement(row);
                        }
                    }
                }

                //filter the requirement which only derived from informative requirement(s)
                foreach(string derivedId in derivedFromInformativeReqs)
                {
                    DerivedRequirement derivedReq = derivedRequirements[derivedId];
                    foreach (string originalId in derivedReq.OriginalReqs)
                    {
                        if (!informativeReqs.Contains(originalId))
                        {
                            validationErrors.Remove(derivedId);
                        }
                    }
                }

                foreach (string invalidOriReq in oriReqOutOfDeltaScope)
                {
                    RemoveDerivedRequirmentRelationship(invalidOriReq);
                }

                requirementTable.Dispose();

                //find the loop in the requirement table and throw exception when loops exist.
                FindLoop();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    String.Format("[ERROR] Unable to get requirement data from specified requirement table files. Details:\r\n{0}", e.Message + e.StackTrace));
            }
        }

        

        private void RemoveDerivedRequirmentRelationship(string oriReqId)
        {
            if (derivedRequirements.ContainsKey(oriReqId))
            {
                DerivedRequirement oReq = derivedRequirements[oriReqId];
                if (oReq.OriginalReqs.Count == 0)
                {
                    derivedRequirements.Remove(oriReqId);
                    if (requirementsToVerify.ContainsKey(oriReqId))
                    {
                        List<string> ltStrings = new List<string>();
                        ltStrings = requirementsToVerify[oriReqId];
                        requirementsToVerify.Remove(oriReqId);
                        requirementsNotToVerify.Add(oriReqId, ltStrings);
                    }
                }

                foreach (string derivedId in oReq.DerivedReqs.Keys)
                {
                    derivedRequirements[derivedId].RemoveOriginalRequirement(oriReqId);
                    RemoveDerivedRequirmentRelationship(derivedId);
                }
            }
        }

        /// <summary>
        /// Requirements to be verified
        /// Key: requirement ID, Values: requirement description,  Doc_sect and Scope
        /// </summary>
        
        public Dictionary<string, List<string>> RequirementsToVerify
        {
            get
            {
                return requirementsToVerify;
            }
        }

        

        /// <summary>
        /// Key: requirement ID, Values: requirement description,  Doc_sect and Scope
        /// </summary>
        public Dictionary<string, List<string>> RequirementsNotToVerify
        {
            get { return requirementsNotToVerify; }
        }

        /// <summary>
        /// Key: requirement ID, Values: requirement description, Doc_sect and scope
        /// </summary>
        public Dictionary<string, List<string>> RequirementsDeleted
        {
            get { return requirementsDeleted; }
        }

        public uint TotalCount;

        
        public uint ToVerifyCount
        {
            get
            {
                return (uint)requirementsToVerify.Count;
            }
        }

        /// <summary>
        /// Graph of derived requirements.
        /// Key: derived requirement ID, 
        /// Value: structure to represent the current derived requirements.
        /// </summary>
        public Dictionary<string, DerivedRequirement> DerivedRequirements
        {
            get
            {
                return derivedRequirements;
            }
        }

        /// <summary>
        /// Gets verification values for all requirements.
        /// </summary>
        public Dictionary<string, string> RequirementVerifications
        {
            get
            {
                return reqVerifications;
            }
        }

        /// <summary>
        /// Gets requirements spec validation errors results.
        /// </summary>
        public Dictionary<string, string> RSValicationErrors
        {
            get
            {
                return validationErrors;
            }
        }

        /// <summary>
        /// Gets requirements spec validation warnings results.
        /// </summary>
        public Dictionary<string, string> RSValidationWarnings
        {
            get
            {
                return validationWarnings;
            }
        }

        public List<string> InformativeRequirements
        {
            get
            {
                return informativeReqs;
            }
        }

        /// <summary>
        /// Gets the total count of original requirements.
        /// </summary>
        public uint TotalOriginalCount
        {
            get
            {
                uint count = 0;
                if (derivedRequirements.Count != 0)
                {
                    foreach (KeyValuePair<string, DerivedRequirement> kvp in derivedRequirements)
                    {
                        if (kvp.Value.OriginalReqs.Count == 0 && 
                            !informativeReqs.Contains(kvp.Key))
                        {
                            count++;
                        }
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// Find the loop in the requirement table. If loop happens, function will throw an exception, and give some information.  
        /// </summary>
        public void FindLoop()
        {
            InitStatus();
            foreach (KeyValuePair<string, DerivedRequirement> kvp in derivedRequirements)
            {
                DFSFindLoop(kvp.Key);
                reqIDVisitStatus[kvp.Key] = VisitStatus.Removed;
            }
        }

        //Find the loop in the graph by depth-first searching from a start require ID.    
        private void DFSFindLoop(string startReq)
        {
            startReqIDTag = false;
            Dictionary<string, string> loopTrace = new Dictionary<string, string>();
            Stack<KeyValuePair<string, string>> DFSStack = new Stack<KeyValuePair<string, string>>();
            if (!AddEdgeOf(startReq, DFSStack))
            {
                return;
            }

            while (DFSStack.Count != 0)
            {
                KeyValuePair<string, string> removeEdge = new KeyValuePair<string, string>();
                removeEdge = DFSStack.Pop();
                if (reqIDVisitStatus[removeEdge.Value] == VisitStatus.Unvisited)
                {
                    if (!loopTrace.ContainsKey(removeEdge.Value))
                    {
                        loopTrace.Add(removeEdge.Value, removeEdge.Key);
                    }

                    if (removeEdge.Value == startReq)
                    {
                        OutPutLoop(removeEdge.Value, loopTrace);
                    }

                    //If A->A1, A->A2, A->A3, when visit the child nodes A1, A2, A3 of A, set A as unvisited.                    
                    if (removeEdge.Key == startReq)
                    {
                        startReqIDTag = false;
                    }

                    AddEdgeOf(removeEdge.Value, DFSStack);
                }
            }
        }

        //Used by DFS add the edge of reqID to the stack.
        private bool AddEdgeOf(string reqID, Stack<KeyValuePair<string, string>> DFSStack)
        {
            if (startReqIDTag == false)
            {
                startReqIDTag = true;
            }
            else
            {
                reqIDVisitStatus[reqID] = VisitStatus.Visited;
            }

            if (derivedRequirements[reqID].OriginalReqs.Count == 0 || derivedRequirements[reqID].DerivedReqs.Count == 0)
            {
                return false;
            }
            else
            {
                foreach (KeyValuePair<string, DerivedType> kvp in derivedRequirements[reqID].DerivedReqs)
                {
                    DFSStack.Push(new KeyValuePair<string, string>(reqID, kvp.Key));
                }
                return true;
            }
        }

        //Output the loop detected to console
        private static void OutPutLoop(string startAndEndID, Dictionary<string, string> loopTrace)
        {
            string exceptionString = string.Empty;

            string parentReqID = startAndEndID;
            do
            {
                exceptionString = exceptionString + parentReqID + " --> ";
                parentReqID = loopTrace[parentReqID];
            }
            while (parentReqID != startAndEndID);
            exceptionString = exceptionString + startAndEndID;
            throw new InvalidOperationException("Found loop in the derived requirements " + exceptionString);
        }

        //initial the visit status
        private void InitStatus()
        {
            foreach (KeyValuePair<string, DerivedRequirement> kvp in derivedRequirements)
            {
                reqIDVisitStatus.Add(kvp.Key, VisitStatus.Unvisited);
            }
        }

        //validate original requirement
        private bool ValidateOriginalRequirement(ReqTable.RequirementRow derivedReq, string originalId)
        {
            bool isLegal = true;
            //error: cannot derive from deleted
            if (requirementsDeleted.ContainsKey(originalId))
            {
                if (!validationErrors.ContainsKey(derivedReq.REQ_ID))
                {
                    validationErrors.Add(derivedReq.REQ_ID, RSValidationRules.DERIVEFROMDELETED);
                }
                isLegal = false;
            }
            //check the original requirement is normative and testable.
            else if (requirementsNotToVerify.ContainsKey(originalId))
            {
                //error: cannot derive from informative
                if (informativeReqs.Contains(originalId))
                {
                    //we ignore informative+unverified requirement, 
                    //and not count this kind of requirement to coverage.
                    if (!validationErrors.ContainsKey(derivedReq.REQ_ID) &&
                        string.Compare(RequirementVerifications[originalId], VerificationValues.UNVERIFIED, true) != 0)
                    {
                        derivedFromInformativeReqs.Add(derivedReq.REQ_ID);
                        validationErrors.Add(derivedReq.REQ_ID, RSValidationRules.DERIVEFROMINFORMATIVE);
                    }
                }
            }
            else
            {
                switch (RequirementVerifications[originalId])
                {
                    case VerificationValues.UNVERIFIED:
                        {
                            //warning: cannot derive from normative, unverified.
                            //should still add to derived requirement table.
                            if (!validationWarnings.ContainsKey(derivedReq.REQ_ID))
                            {
                                validationWarnings.Add(derivedReq.REQ_ID, RSValidationRules.DERIVEFROMUNVERIFIED);
                            }
                            break;
                        }
                    default:
                        {
                            //do nothing
                            break;
                        }
                }
            }

            return isLegal;
        }

        //validate derived requirement
        private bool ValidateDerivedRequirement(ReqTable.RequirementRow derivedReq)
        {
            //ignore original requirement without derivation.
            if (derivedReq.IsDerivedNull() ||
                string.IsNullOrEmpty(derivedReq.Derived.Trim()))
            {
                return false;
            }

            //////////////////////if ((deltaScopes.Count > 0 && !deltaScopes.Contains(derivedReq.Delta)))
            //////////////////////{
            //////////////////////    return false;
            //////////////////////}

            bool validateResult = true;
            if (!requirementsToVerify.ContainsKey(derivedReq.REQ_ID))
            {
                if (!validationErrors.ContainsKey(derivedReq.REQ_ID))
                {
                    //error: derived requirement cannot be informative
                    if (informativeReqs.Contains(derivedReq.REQ_ID))
                    {
                        validationErrors.Add(derivedReq.REQ_ID, RSValidationRules.DERIVEDREQISINFORMATIVE);
                    }
                    //error: derived requirement cannot be out-of-scope
                    else if (scopeRules[ReportingParameters.outScopeRule].Contains(derivedReq.Scope.ToLower()))
                    {
                        validationErrors.Add(derivedReq.REQ_ID, RSValidationRules.DERIVEDREQOUTOFSCOPE);

                    }
                    //error: derived requirement is deleted, non-testable or unverified.
                    else if (string.Compare(derivedReq.Verification, VerificationValues.DELETED) == 0)
                    {
                        validationErrors.Add(derivedReq.REQ_ID, RSValidationRules.DERIVEDREQNOTTESTABLE);
                        validateResult = false;
                    }
                    else if (string.Compare(derivedReq.Verification, VerificationValues.NONTESTABLE) == 0 ||
                        string.Compare(derivedReq.Verification, VerificationValues.UNVERIFIED) == 0)
                    {
                        //derived requirement can only be tast case or adapter.
                        validationErrors.Add(derivedReq.REQ_ID, RSValidationRules.DERIVEDREQNOTTESTABLE);
                    }
                }
            }

            return validateResult;
        }

        /// <summary>
        /// Try find ID in Req_Id column
        /// </summary>
        /// <param name="reqId">id in other column</param>
        /// <param name="isPartial">whether the input id is partial id</param>
        /// <returns>if match return the id in Req_Id column, otherwise return empty string</returns>
        //TODO:Sould make sure no duplicate requirement ids in RS.
        public string GetRequirementId(string reqId, bool isPartial)
        {
            //only check id in Req_Id column
            if (reqVerifications.ContainsKey(reqId))
            {
                return reqId;
            }

            //reqVerifications contains all Req_Id and Verification column values in RS.
            foreach (string rsId in reqVerifications.Keys)
            {
                string fullReqId = reqId;
                if (isPartial)
                {
                    //build the partial req id to full req id.
                    fullReqId = GetFullReqId(reqId);
                }

                //match
                if (string.Compare(GetFullReqId(rsId), fullReqId, true) == 0)
                {
                    return rsId;
                }
            }

            return string.Empty;
        }

        //create the full req id by prefix and partial id.
        private string GetFullReqId(string partialId)
        {
            string fullReqId = partialId;
            if (this.prefix != null)
            {
                for (int index = 0; index <= this.prefix.Length; index++)
                {
                    string subPrefix = this.prefix.Substring(0, index);
                    string partialIdHead = this.prefix.Substring(index);
                    if (partialId.StartsWith(partialIdHead, StringComparison.CurrentCultureIgnoreCase))
                    {
                        fullReqId = subPrefix + partialId;
                        break;
                    }
                }
            }

            return fullReqId;
        }

        private void MakeSureNoDuplicateId()
        {
            Dictionary<string, string> allIds = new Dictionary<string, string>();

            foreach (string id in reqVerifications.Keys)
            {
                string fullId = GetFullReqId(id);
                if (allIds.ContainsKey(fullId))
                {
                    throw new InvalidOperationException(
                            string.Format("Found two ids in Requirement Specification '{0}','{1}' which Reporting Tool treated as duplicated",
                            allIds[fullId], id));
                }
                else
                {
                    allIds.Add(fullId, id);
                }
            }
            //string[] allIds = new string[reqVerifications.Count];
            //int index = 0;
            //foreach (string id in reqVerifications.Keys)
            //{
            //    allIds[index] = id;
            //    index++;
            //}

            //for (int i = 0; i < index; i++)
            //    for (int j = i + 1; j < allIds.Length; j++)
            //    {
            //        if (string.Compare(GetFullReqId(allIds[i]), GetFullReqId(allIds[j]), true) == 0)
            //        {
            //            throw new InvalidOperationException(
            //                string.Format("Find two ids in RS '{0}','{1}' which RT treated as duplicated",
            //                allIds[i], allIds[j]));
            //        }
            //    }
        }
    }
}
