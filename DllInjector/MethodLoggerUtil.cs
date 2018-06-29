using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PERWAPI;

namespace MethodLogger
{
    static class MethodLoggerUtil
    {
        static readonly string startStaticLogMethodName = "StaticMethodStarted";
        static readonly string endStaticLogMethodName = "StaticMethodCompleted";
        static readonly string startLogMethodName = "MethodStarted";
        static readonly string endLogMethodName = "MethodCompleted";

        static readonly string ClassContainsLogMethodsName = "LogMethods";
        static readonly string AssemblyContainsLogMethodsName = "Logger";

        static readonly HashSet<string> ignoredMethodNames = new HashSet<string>
        {
            "Main",
            ".ctor"
        };
            

        private static bool CheckLogMethodParameters(PERWAPI.Type[] types)
        {
            if (types.Length == 0)
                return true;

            return false;
        }

        private static bool GetLoggerMethodsFromClass(ClassRef methodLogger, 
                                                      out Method startLogMethod, out Method endLogMethod,
                                                      out Method staticStartLogMethod, out Method staticEndLogMethod)
        {
            startLogMethod = endLogMethod = staticStartLogMethod = staticEndLogMethod =  null;

            MethodRef tempStartLogMethod = methodLogger.GetMethod(startLogMethodName);
            MethodRef tempEndLogMethod = methodLogger.GetMethod(endLogMethodName);
            MethodRef tmpStaticStartLogMethod = methodLogger.GetMethod(startStaticLogMethodName);
            MethodRef tmpStaticEndLogMethod = methodLogger.GetMethod(endStaticLogMethodName);

            if (tempStartLogMethod != null && tempEndLogMethod != null && tmpStaticStartLogMethod != null && tmpStaticEndLogMethod != null)
            {
                if (CheckLogMethodParameters(tempStartLogMethod.GetParTypes()) && 
                    CheckLogMethodParameters(tempEndLogMethod.GetParTypes()) &&
                    CheckLogMethodParameters(tmpStaticStartLogMethod.GetParTypes()) &&
                    CheckLogMethodParameters(tmpStaticEndLogMethod.GetParTypes()))
                {
                    startLogMethod = tempStartLogMethod;
                    endLogMethod = tempEndLogMethod;
                    staticStartLogMethod = tmpStaticStartLogMethod;
                    staticEndLogMethod = tmpStaticEndLogMethod;
                    return true;
                }
            }

            return false;
        }

        private static bool GetLoggerMethodsFromClass(ClassDef methodLogger, out Method startLogMethod, out Method endLogMethod,
                                                                             out Method staticStartLogMethod, out Method staticEndLogMethod)
        {
            startLogMethod = endLogMethod = staticStartLogMethod = staticEndLogMethod = null;

            Method tempStartLogMethod = methodLogger.GetMethodDesc(startLogMethodName);
            Method tempEndLogMethod = methodLogger.GetMethodDesc(endLogMethodName);
            Method tempStaticStartLogMethod = methodLogger.GetMethodDesc(startStaticLogMethodName);
            Method tempStaticEndLogMethod = methodLogger.GetMethodDesc(endStaticLogMethodName);

            if (tempStartLogMethod != null && tempEndLogMethod != null)
            {
                if (CheckLogMethodParameters(tempStartLogMethod.GetParTypes()) && 
                    CheckLogMethodParameters(tempEndLogMethod.GetParTypes()) &&
                    CheckLogMethodParameters(tempStaticStartLogMethod.GetParTypes()) &&
                    CheckLogMethodParameters(tempStaticEndLogMethod.GetParTypes()))
                {
                    startLogMethod = tempStartLogMethod;
                    endLogMethod = tempEndLogMethod;
                    staticStartLogMethod = tempStaticStartLogMethod;
                    staticEndLogMethod = tempStaticEndLogMethod;
                    return true;
                }
            }

            return false;
        }

        public static bool LocateLoggerMethods(PEFile file, string assemblyName, string className, 
                           out Method staticStartLogMethod, out Method staticEndLogMethod,
                           out Method startLogMethod, out Method endLogMethod)
        {
            startLogMethod = endLogMethod = staticStartLogMethod = staticEndLogMethod = null;

            // Check if it is in this assembly itself
            var a = file.GetThisAssembly().Name();
            if (file.GetThisAssembly().Name() == assemblyName)
            {
                ClassDef methodLogger = file.GetClass(className);

                if (methodLogger != null)
                {
                    return GetLoggerMethodsFromClass(methodLogger, out startLogMethod, out endLogMethod , 
                                                                   out staticStartLogMethod, out staticEndLogMethod);
                }
            }

            // Check referenced assemblies
            foreach (AssemblyRef assemblyRef in file.GetImportedAssemblies())
            {
                var b = assemblyRef.Name();
                if (assemblyRef.Name() == assemblyName)
                {
                    ClassRef methodLoggerRef = TryGetMethodLoggerFromAssembly(assemblyRef, className);
                    if (methodLoggerRef != null)
                    {
                        if (GetLoggerMethodsFromClass(methodLoggerRef, out startLogMethod, out endLogMethod,
                                                                       out staticStartLogMethod, out staticEndLogMethod))
                            return true;
                    }
                }
            }

            // Not found in this assembly or referenced assemblies. Try loading given assembly and adding it as reference
            AssemblyRef newAssemblyRef = file.MakeExternAssembly(assemblyName);
            ClassRef newMethodLoggerRef = TryGetMethodLoggerFromAssembly(newAssemblyRef, className);
            if (newMethodLoggerRef != null)
            {
                if (GetLoggerMethodsFromClass(newMethodLoggerRef, out startLogMethod, out endLogMethod,
                                                                  out staticStartLogMethod, out staticEndLogMethod))
                    return true;
            }
            return false;
        }

        private static ClassRef TryGetMethodLoggerFromAssembly(AssemblyRef assemblyRef, string className)
        {
            string fileName = assemblyRef.Name() + ".dll";
            var fileFound = true;
            if (!File.Exists(fileName))
            {
                Console.WriteLine(fileName + " not present in current directory. Skipping it in search");
                Console.WriteLine("Trying with *.exe extention");
                fileFound = false;
            }
            if(!fileFound)
            {
                fileName = assemblyRef.Name() + ".exe";
                if (!File.Exists(fileName))
                {
                    Console.WriteLine(fileName + " not present in current directory. Skipping it in search");
                    return null;
                }
            }
            
            PEFile refFile = PEFile.ReadPEFile(fileName);
            ClassDef methodLogger = refFile.GetClass(className);

            if (methodLogger != null)
            {
                ClassRef methodLoggerRef = methodLogger.MakeRefOf();
                if (assemblyRef.GetClass(className) == null)
                {
                    assemblyRef.AddClass(methodLoggerRef);
                }

                System.Array.ForEach(methodLogger.GetMethods(), delegate (MethodDef methodDef)
                {
                    if (methodLoggerRef.GetMethod(methodDef.Name()) == null)
                    {
                        methodLoggerRef.AddMethod(methodDef.Name(), methodDef.GetRetType(), methodDef.GetParTypes());
                    }
                });
                refFile.WritePEFile(true);
                return methodLoggerRef;
            }
            return null;
        }

        public static void ProcessFile(string inputFile, string assemblyContainsLogMethodsName = null, string classContainsLogMethodsName = null)
        {

            if (string.IsNullOrEmpty(assemblyContainsLogMethodsName))
            {
                assemblyContainsLogMethodsName = AssemblyContainsLogMethodsName;
            }

            if (string.IsNullOrEmpty(classContainsLogMethodsName))
            {
                classContainsLogMethodsName = ClassContainsLogMethodsName;
            }

            string directoryName = Path.GetDirectoryName(inputFile);
            if (!string.IsNullOrEmpty(directoryName) && !string.IsNullOrWhiteSpace(directoryName))
            {
                Environment.CurrentDirectory = directoryName;
                inputFile = Path.GetFileName(inputFile);
            }


            PEFile file = PEFile.ReadPEFile(inputFile);

            Method startLogMethod, endLogMethod, staticStartLogMethod, staticEndLogMethod;
            if (!LocateLoggerMethods(file, assemblyContainsLogMethodsName, classContainsLogMethodsName, out startLogMethod, out endLogMethod,
                                                                                                        out staticStartLogMethod, out staticEndLogMethod))
            {
                return;
            }

            ClassDef[] classes = file.GetClasses();

            System.Array.ForEach(classes, delegate (ClassDef classDef)
            {
                ProcessClass(classDef, startLogMethod, endLogMethod, staticStartLogMethod , staticEndLogMethod, classContainsLogMethodsName);
            });

            file.WritePEFile(true);
        }

        private static void ProcessClass(ClassDef classDef, Method startLogMethod, Method endLogMethod, Method staticStartLogMethod, Method staticEndLogMethod,  string methodLoggerClassName)
        {
            // Don't modify the class methods that we are going to emit calls to, otherwise we'll get unbounded recursion.
            if (classDef.Name() == methodLoggerClassName)
                return;

            foreach (NestedClassDef c in classDef.GetNestedClasses())
            {
                ProcessClass(c, startLogMethod, endLogMethod, staticStartLogMethod, staticEndLogMethod, methodLoggerClassName);
            }

            foreach (MethodDef methodDef in classDef.GetMethods())
            {
                ModifyCode(classDef, methodDef, startLogMethod, endLogMethod, staticStartLogMethod, staticEndLogMethod);
            }
        }

        private static void ModifyCode(ClassDef classDef, MethodDef methodDef, Method staticStartLogMethod, Method staticEndLogMethod, Method startLogMethod, Method endLogMethod)
        {
            if (ignoredMethodNames.Contains(methodDef.Name())) return;

            CILInstructions instructions = methodDef.GetCodeBuffer();

            instructions.StartInsert();

            //instructions.StartBlock();
            instructions.MethInst(MethodOp.call, staticStartLogMethod);
            //TryBlock tryBlock1 = instructions.EndTryBlock();
            //instructions.StartBlock();
            //instructions.EndFaultBlock(tryBlock1);
            //instructions.StartBlock();
            instructions.EndInsert();


            while (instructions.GetNextInstruction().GetPos() < instructions.NumInstructions() - 2) ;

            instructions.StartInsert();

            //instructions.StartBlock();
            instructions.MethInst(MethodOp.call, staticEndLogMethod);
            //TryBlock tryBlock2 = instructions.EndTryBlock();
            //instructions.StartBlock();
            //instructions.EndFinallyBlock(tryBlock2);
            instructions.EndInsert();
        }
    }
}
