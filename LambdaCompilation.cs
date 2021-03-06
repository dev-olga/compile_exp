﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lambda
{
    using System.CodeDom.Compiler;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text.RegularExpressions;

    using Microsoft.CSharp;

    public static class LambdaCompilation
    {

        private static readonly string NamespaceName = "LambdaExp";
        private static readonly string ClassName = "LambdaClass";

        public static Expression<Func<T, TResult>> CompileLambdaExpr<T, TResult>(string lambdaString)
        {
            string source = LambdaClassWrapper(GetFuncString<T, TResult>(), lambdaString, true);

            CompilerResults results = CompileAssemblyFromSource(source);
            return GetLambda<Expression<Func<T, TResult>>>(results);

        }
        
        public static Action<T> CompileLambda<T>(string lambdaString)
        {
            string source = LambdaClassWrapper("Action<" + TypeToString(typeof(T)) + ">", lambdaString);

            CompilerResults results = CompileAssemblyFromSource(source);
            return GetLambda<Action<T>>(results);
            
        }

        public static Func<T, TResult> CompileLambda<T, TResult>(string lambdaString)
        {
            string source = LambdaClassWrapper(GetFuncString<T, TResult>(), lambdaString);

            CompilerResults results = CompileAssemblyFromSource(source);
            return GetLambda<Func<T, TResult>>(results);
        }

        private static string TypeToString(Type type)
        {
            var res = type.ToString();
            res = new Regex(@"`\d+\[").Replace(res, "<");
            res = new Regex(@"\]").Replace(res, ">");
            return res;
        }

        private static CompilerResults CompileAssemblyFromSource(string source)

        {
            CompilerParameters parameters = new CompilerParameters();

            Assembly oCurrentAssembly = typeof(Program).Assembly;
            foreach (AssemblyName oName in oCurrentAssembly.GetReferencedAssemblies())
            {
                var strLocation = Assembly.ReflectionOnlyLoad(oName.FullName).Location;
                parameters.ReferencedAssemblies.Add(strLocation);
            }
            string exeName = Assembly.GetEntryAssembly().Location;
            parameters.ReferencedAssemblies.Add(exeName);

            parameters.GenerateExecutable = false;
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            ICodeCompiler icc = codeProvider.CreateCompiler();


            CompilerResults results = icc.CompileAssemblyFromSource(parameters, source);
            return results;
        }

        private static T GetLambda<T>(CompilerResults results)
            where T: class
        {
            if (!results.Errors.HasErrors && !results.Errors.HasWarnings)
            {
                Assembly ass = results.CompiledAssembly;

                object lambdaClassInstance =
                        ass.CreateInstance(NamespaceName + "." + ClassName);

                object lambdaMethodResult =
                       lambdaClassInstance.GetType().GetProperty("Prop").GetValue(lambdaClassInstance);

                return lambdaMethodResult as T;
            }
            return null;
        }

        private static string LambdaClassWrapper(string type, string exp, bool expression = false)
        {
            string source =
                    "using System;" +
                    "using System.Linq.Expressions;" +
                    "namespace " + NamespaceName +
                    "{" +
                        "public class " + ClassName +
                        "{" +
                            (expression ? ExpressionWrapper(type) : type) + " field = " + exp+"; " +
                            "public "+ (expression ? ExpressionWrapper(type) : type) + " Prop {get { return field; }}" +
                        "}" +
                    "}";
            return source;
        }

        private static string ExpressionWrapper(string type)
        {
            return "Expression<" + type + ">";
        }

        private static string GetFuncString<T, TResult>()
        {
            return "Func<" + TypeToString(typeof(T)) + ", " + TypeToString(typeof(TResult)) + ">";
        }
    }
}
