// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// Translates the data from txt to js as json, adds category to the test cases
    /// </summary>
    public class TxtToJSON
    {
        // The path to category.js which stores the map between case name and category
        public string CaseCategoryFile = Path.Combine(Directory.GetCurrentDirectory(), "category.js");
        // Path list to Binary files of the test cases
        private static List<string> dllFiles = new List<string>();
        // Stores the map between case name and class
        private Dictionary<string, string> caseClass = new Dictionary<string, string>();
        // Auxiliary variable to save information as JSON
        private JavaScriptSerializer serializer = new JavaScriptSerializer();

        private enum LogType
        {
            None = -1,
            ErrorStackTrace = 1,
            ErrorMessage = 2,
            StandardOut = 3
        };

        /// <summary>
        /// Translates List<DataType.TestCase> to DataType.TestCasesSummary string
        /// </summary>
        /// <param name="resultFolder">Test result folder, in which log is stored as txt</param>
        /// <param name="captureFolder">The path to the capture files need to be placed</param>
        /// <returns>Returns detailed test cases information</returns>
        public string TestCasesString(string resultFolder, string captureFolder)
        {
            List<DataType.TestCase> testCaseList = GetTestCaseList(resultFolder, captureFolder);
            DataType.TestCasesSummary rs = new DataType.TestCasesSummary()
            {
                TestCases = testCaseList,
                TestCasesCategories = GetTestCaseCategortyList(testCaseList),
                TestCasesClasses = GetTestCaseClassList(testCaseList)
            };
            return (serializer.Serialize(rs));
        }

        /// <summary>
        /// Constructs details of test case.
        /// 1. Gets log from the txt file
        /// 2. Copies capture to the log folder, and saves the path.
        /// </summary>
        /// <param name="txtfile">The path to the log txt file which contains the detail log</param>
        /// <returns>Returns the log information</returns>
        public string ConstructCaseDetail(string txtfile, string captureFolder)
        {
            FileStream fs = new FileStream(txtfile, FileMode.Open);
            string type = ""; // Help judge whether the line is the end of the file, help distinguish file content
            StreamReader sr = new StreamReader(fs);
            string line;
            string content = "";
            string standardOutType = "";
            DataType.TestCaseDetail caseDetail = new DataType.TestCaseDetail(sr.ReadLine(), sr.ReadLine(), sr.ReadLine(), sr.ReadLine());
            DataType.StandardOutDetail stdDetail;
            string dllFile = sr.ReadLine();
            if (!dllFiles.Contains(dllFile))
            {
                dllFiles.Add(dllFile);
            }
            while ((line = sr.ReadLine()) != null)
            {
                if (line == "") //Rule out blank line
                    continue;

                if (line.StartsWith("==========="))
                {
                    type = line.Replace("=", "");
                    continue;
                }

                LogType eType = (LogType)Enum.Parse(typeof(LogType), type);
                switch (eType)
                {
                    case LogType.ErrorStackTrace:
                        caseDetail.ErrorStackTrace.Add(line);
                        break;
                    case LogType.ErrorMessage:
                        caseDetail.ErrorMessage.Add(line);
                        break;
                    case LogType.StandardOut:
                        int begin = line.IndexOf('[');
                        int end = line.IndexOf(']');

                        if (begin != -1 && end != -1)
                        {
                            if (standardOutType != "")
                            {
                                stdDetail = new DataType.StandardOutDetail()
                                {
                                    Content = content,
                                    Type = standardOutType
                                };
                                caseDetail.StandardOut.Add(stdDetail);
                            }
                            if (end > begin && end < line.Length)
                            {
                                standardOutType = line.Substring(begin + 1, end - begin - 1);
                                if (!caseDetail.StandardOutTypes.Contains(standardOutType))
                                    caseDetail.StandardOutTypes.Add(standardOutType);
                            }
                            content = line;
                        }
                        else
                            content += line;
                        break;
                    default: break;
                }
            }
            stdDetail = new DataType.StandardOutDetail()
            {
                Content = content,
                Type = standardOutType
            };
            fs.Close();
            caseDetail.StandardOut.Add(stdDetail);

            caseDetail.CapturePath = CopyCaptureAndReturnPath(caseDetail.Name, captureFolder);

            return (serializer.Serialize(caseDetail));
        }

        /// <summary>
        /// Gets the statistical information
        /// </summary>
        /// <param name="totalCasesNum">The number of total cases</param>
        /// <param name="passedNum">The number of passed cases</param>
        /// <param name="failedNum">The number of failed cases</param>
        /// <param name="testRunStartTime">The start time of the run</param>
        /// <param name="testRunEndTime">The end time of the run</param>
        /// <returns>Return statistical information about this test</returns>
        public string SummaryTable(long totalCasesNum,
                               long passedNum,
                               long failedNum,
                               DateTimeOffset testRunStartTime,
                               DateTimeOffset testRunEndTime)
        {
            DataType.RunSummary sry = new DataType.RunSummary()
            {
                TotalCount = totalCasesNum,
                FailedCount = failedNum,
                PassedCount = passedNum,
                InconclusiveCount = totalCasesNum - passedNum - failedNum,
                PassRate = totalCasesNum == 0 ? 0 : (float)passedNum * 100 / totalCasesNum,
                StartTime = testRunStartTime.ToLocalTime().ToString("MM/dd/yyyy HH:mm:ss"),
                EndTime = testRunEndTime.ToLocalTime().ToString("MM/dd/yyyy HH:mm:ss"),
                Duration = testRunEndTime.Subtract(testRunStartTime).ToString(@"hh\:mm\:ss")
            };
            return (serializer.Serialize(sry));
        }

        /// <summary>
        /// Generates all test cases categories js file
        /// </summary>
        private void GenerateCaseCategoryFile()
        {
            Dictionary<string, List<string>> testCases = GetTestCaseCategories();
            string sTestCases = serializer.Serialize(testCases);
            File.WriteAllText(CaseCategoryFile, sTestCases);
        }

        /// <summary>
        /// Gets test case categories by analyzing the test dll files
        /// </summary>
        /// <returns>Returns case name(key) and categories(value)</returns>
        private Dictionary<string, List<string>> GetTestCaseCategories()
        {
            Dictionary<string, List<string>> testCases = new Dictionary<string, List<string>>();
            DirectoryInfo info = new DirectoryInfo(Directory.GetCurrentDirectory());
            bool existBatch = false;
            if (info.FullName.EndsWith("Batch"))
                existBatch = true;

            string fullPath = existBatch ? info.Parent.FullName : info.FullName;
            foreach (string dllPath in dllFiles)
            {
                string dllFile = dllPath;
                if (string.IsNullOrEmpty(dllFile))
                {
                    continue;
                }
                if (dllPath.StartsWith(".."))
                {
                    dllFile = dllPath.Substring(3);
                    dllFile = Path.Combine(fullPath, dllFile);
                }
                try
                {
                    Assembly assembly = Assembly.LoadFrom(dllFile);
                    Type[] types = assembly.GetTypes();
                    foreach (Type type in types)
                    {
                        //search for class, out interfaces and other type
                        if (!type.IsClass)
                        {
                            continue;
                        }

                        MethodInfo[] methods = type.GetMethods();

                        foreach (MethodInfo method in methods)
                        {
                            //methods loop, search for methods with TestMethodAttribute
                            object[] objs = method.GetCustomAttributes(false);
                            foreach (object obj in objs)
                            {
                                //TestMethods
                                if (obj.GetType().Name != "TestMethodAttribute" || testCases.ContainsKey(method.Name))
                                {
                                    continue;
                                }
                                //GetCategory
                                List<string> categories = new List<string>();
                                foreach (object attribute in objs)
                                {
                                    //record TestCategories
                                    if (attribute.GetType().Name == "TestCategoryAttribute")
                                    {
                                        PropertyInfo property = attribute.GetType().GetProperty("TestCategories");
                                        object category = property.GetValue(attribute, null);
                                        foreach (string str in (System.Collections.ObjectModel.ReadOnlyCollection<string>)category)
                                        {
                                            categories.Add(str);
                                        }
                                    }
                                }
                                testCases.Add(method.Name, categories);
                                caseClass.Add(method.Name, type.Name);
                            }
                        }
                    }
                }
                catch
                {
                    System.Console.WriteLine("skip {0}", dllFile);
                }
            }
            return testCases;
        }

        /// <summary>
        /// Gets the test case list with basic information
        /// </summary>
        /// <param name="resultFolder">The path to the result folder</param>
        /// <param name="captureFolder">The path to the capture folder</param>
        /// <returns>Returns the test case list with basic information</returns>
        private List<DataType.TestCase> GetTestCaseList(string resultFolder, string captureFolder)
        {
            List<DataType.TestCase> testCaseList = new List<DataType.TestCase>();

            if (!File.Exists(CaseCategoryFile))
            {
                GenerateCaseCategoryFile();
            }

            string sJSON = File.ReadAllText(CaseCategoryFile);
            Dictionary<string, List<string>> testCases = serializer.Deserialize<Dictionary<string, List<string>>>(sJSON);
            string[] txtfiles = Directory.GetFiles(resultFolder);
            foreach (var file in txtfiles)
            {
                string caseName = Path.GetFileNameWithoutExtension(file);
                int i = caseName.IndexOf('_');
                caseName = caseName.Substring(i + 1); // out the sort id
                i = caseName.IndexOf('_');
                string caseStatus = caseName.Substring(0, i);
                caseName = caseName.Substring(i + 1);
                DataType.TestCase tc = new DataType.TestCase()
                {
                    Name = caseName,
                    Result = caseStatus,
                    ClassType = caseClass.ContainsKey(caseName) ? caseClass[caseName] : null,
                    Category = testCases.ContainsKey(caseName) ? testCases[caseName] : null,
                };
                testCaseList.Add(tc);
            }
            return testCaseList;
        }

        /// <summary>
        /// Gets the test case category list
        /// </summary>
        /// <param name="testCaseList">The test cases</param>
        /// <returns>Returns test cases categories</returns>
        private List<string> GetTestCaseCategortyList(List<DataType.TestCase> testCaseList)
        {
            List<string> listCaseCategory = new List<string>();
            foreach (DataType.TestCase testCase in testCaseList)
            {
                listCaseCategory.AddRange(testCase.Category);
            }
            listCaseCategory = listCaseCategory.Distinct().ToList();
            listCaseCategory.Sort();
            return listCaseCategory;
        }

        /// <summary>
        /// Gets the test case class list
        /// </summary>
        /// <param name="testCaseList">The test cases</param>
        /// <returns>Returns the test cases class list</returns>
        private List<string> GetTestCaseClassList(List<DataType.TestCase> testCaseList)
        {
            List<string> listCaseClass = new List<string>();
            foreach (DataType.TestCase testCase in testCaseList)
            {
                listCaseClass.Add(testCase.ClassType);
            }
            listCaseClass = listCaseClass.Distinct().ToList();
            listCaseClass.Sort();
            return listCaseClass;
        }

        /// <summary>
        /// Gets the path to the capture file folder from .ptfconfig file
        /// </summary>
        /// <returns>Returns a list of capture file path when the NetworkCapture is enabled. Returns null when the NetworkCapture is not enabled.</returns>
        private List<string> GetCaptureFilesPath()
        {
            List<string> captureFolders = new List<string>();
            DirectoryInfo info = new DirectoryInfo(Directory.GetCurrentDirectory());
            bool existBatch = false;
            if (info.FullName.EndsWith("Batch"))
                existBatch = true;
            string fullPath = existBatch ? info.Parent.FullName : info.FullName;
            string cfgFolder = Path.Combine(fullPath, "bin");
            try
            {
                string[] ptfconfigFiles = Directory.GetFiles(cfgFolder, "*.ptfconfig", SearchOption.TopDirectoryOnly);
                foreach (string configFile in ptfconfigFiles)
                {
                    XmlDocument configXml = new XmlDocument();
                    configXml.Load(configFile);

                    XmlNodeList groupNodes = configXml.GetElementsByTagName("Group");
                    if (groupNodes == null || groupNodes.Count <= 0) { continue; }

                    bool isNetworkCaptureEnabled = false;
                    string captureFileFolder = null;
                    foreach (XmlNode gNode in groupNodes)
                    {
                        if (gNode.Attributes["name"].Value == "NetworkCapture")
                        {
                            foreach (XmlNode pNode in gNode.ChildNodes)
                            {
                                if (pNode.Attributes["name"].Value == "Enabled")
                                {
                                    isNetworkCaptureEnabled = bool.Parse(pNode.Attributes["value"].Value);
                                }
                                if (pNode.Attributes["name"].Value == "CaptureFileFolder")
                                {
                                    captureFileFolder = pNode.Attributes["value"].Value;
                                }
                            }
                            break;
                        }
                    }

                    if (!isNetworkCaptureEnabled) { continue; }

                    if (!string.IsNullOrEmpty(captureFileFolder))
                    {
                        captureFolders.Add(captureFileFolder);
                    }
                }
            }
            catch
            {
                return captureFolders;
            }
            return captureFolders;
        }

        /// <summary>
        /// Copies a specified capture file to the log folder and returns the destination path.
        /// </summary>
        /// <param name="caseName">The specified case name</param> 
        /// <param name="captureFolder">The path of the destination capture folder</param>
        /// <returns>Returns the destination path of the specified case name</returns>
        private string CopyCaptureAndReturnPath(string caseName, string captureFolder)
        {
            List<string> srcCaptureFolders = GetCaptureFilesPath();

            foreach (string srcCapturePath in srcCaptureFolders)
            {
                if (string.IsNullOrEmpty(srcCapturePath) || !Directory.Exists(srcCapturePath))
                {
                    continue;
                }
                string[] files = Directory.GetFiles(srcCapturePath, string.Format("*{0}.etl", caseName));
                foreach (var file in files)
                {
                    string captureName = Path.GetFileNameWithoutExtension(file); // Get file name
                    captureName = captureName.Substring(captureName.IndexOf('#') + 1);
                    if (String.Compare(captureName, caseName, true) != 0)
                        continue;
                    string desCapturePath = Path.Combine(captureFolder, Path.GetFileName(file));
                    if (File.Exists(desCapturePath))
                    {
                        File.Delete(desCapturePath);
                    }
                    File.Copy(file, desCapturePath);
                    return desCapturePath;
                }
            }

            return null;
        }
    }
}
