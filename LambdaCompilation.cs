using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lambda
{
    using System.CodeDom.Compiler;
    using System.Linq.Expressions;
    using System.Reflection;

    using Microsoft.CSharp;

    public static class LambdaCompilation
    {

        private static readonly string NamespaceName = "LambdaExp";
        private static readonly string ClassName = "LambdaClass";

        public static Expression<Func<T, TResult>> CompileLambdaExpr<T, TResult>(string lambdaExpression)
        {
            string source = LambdaExprClassWrapper(
               "Func<" + typeof(T) + ", " + typeof(TResult) + ">",
                lambdaExpression);

            CompilerResults results = CompileAssemblyFromSource(source);
            return GetExpression<Expression<Func<T, TResult>>>(results);

        }
        
        public static Action<T> CompileLambda<T>(string lambdaExpression)
        {
            string source = LambdaClassWrapper(
                "Action<" + typeof(T)+ ">",
                lambdaExpression);

            CompilerResults results = CompileAssemblyFromSource(source);
            return GetExpression<Action<T>>(results);
            
        }

        public static Func<T, TResult> CompileLambda<T, TResult>(string lambdaExpression)
        {
            string source = LambdaClassWrapper("Func<" + typeof(T) + ", " + typeof(TResult) + ">", lambdaExpression);

            CompilerResults results = CompileAssemblyFromSource(source);
            return GetExpression<Func<T, TResult>>(results);
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

        private static T GetExpression<T>(CompilerResults results)
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

        private static string LambdaClassWrapper(string type, string exp)
        {
            string source =
                    "using System;" +
                    "namespace " + NamespaceName +
                    "{" +
                        "public class " + ClassName +
                        "{" +
                            type + " field = "+ exp+"; " +
                            "public "+ type+" Prop {get { return field; }}" +
                        "}" +
                    "}";
            return source;
        }

        //TODO: refactor
        private static string LambdaExprClassWrapper(string type, string exp)
        {
            string source =
                    "using System;  using System.Linq.Expressions;" +
                    "namespace " + NamespaceName +
                    "{" +
                        "public class " + ClassName +
                        "{" +
                            "Expression<" + type + "> fieldExp = " + exp + "; " +
                            "public Expression<" + type + "> Prop {get { return fieldExp; }}" +
                        "}" +
                    "}";
            return source;
        }
    }
}
