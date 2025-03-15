using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.IO;

namespace ClassLibraryGenerator
{
    [Generator]
    public class SomethingStructGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new ITweenerSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            const string LOG_FILE_PATH = "C:\\Users\\Administrator\\Desktop\\SourceGenErrors.txt";

            try
            {
                if (!(context.SyntaxReceiver is ITweenerSyntaxReceiver receiver))
                    return;

                string debugContent = "";

                var compilation = context.Compilation;

                foreach (var structDeclaration in receiver.Syntaxes)
                {
                    string tweenerNamespace = this.GetNamespace(structDeclaration);
                    string tweenerName = structDeclaration.Identifier.ToString();

                    var genericArguments = ((GenericNameSyntax)structDeclaration.BaseList.Types[0].Type) // BUG: This only works if tweener implement only one interface
                        .TypeArgumentList.Arguments;

                    SemanticModel semanticModel = compilation.GetSemanticModel(genericArguments[0].SyntaxTree);

                    this.GetNameAndNamespaceOfGenericArgument(semanticModel, genericArguments[0]
                        , out string componentTypeName, out string componentNamespace);

                    this.GetNameAndNamespaceOfGenericArgument(semanticModel, genericArguments[1]
                        , out string targetTypeName, out string targetNamespace);

                    this.GeneratePartialPartTweener(context, tweenerName, tweenerNamespace);
                    this.GenerateCanTweenTag(context, tweenerName, componentNamespace);
                    this.GenerateTweenData(context, tweenerName, targetTypeName, targetNamespace, componentNamespace);
                    this.GenerateTweenSystem(context, tweenerName, tweenerNamespace, componentTypeName, componentNamespace);

                    debugContent += $"[{tweenerName}-{tweenerNamespace}], [{componentTypeName}-{componentNamespace}], [{targetTypeName}-{targetNamespace}]";

                }

                string sourceCode = $@"
using System;
namespace SomeNamespace
{{
    public static class Debugger
    {{
        public static string DebugContent = ""Error:[{debugContent}]"";
    }}  
}}
";

                context.AddSource("Debugger.g.cs", sourceCode);

            }
            catch (Exception e)
            {
                File.AppendAllText(LOG_FILE_PATH, $"Source generator error:\n{e}\n");
            }
            
        }

        private string GetNamespace(SyntaxNode syntaxNode)
        {
            SyntaxNode parent = syntaxNode.Parent;

            while (parent != null)
            {
                if (parent is NamespaceDeclarationSyntax namespaceDeclaration)
                    return namespaceDeclaration.Name.ToString();

                parent = parent.Parent;

            }

            return null;

        }

        private void GetNameAndNamespaceOfGenericArgument(
            SemanticModel semanticModel
            , ExpressionSyntax expressionSyntax
            , out string typeName
            , out string namespaceName)
        {
            ITypeSymbol typeSymbol = semanticModel.GetTypeInfo(expressionSyntax).Type;
            if (typeSymbol != null)
            {
                typeName = typeSymbol.Name;
                namespaceName = typeSymbol.ContainingNamespace?.ToDisplayString() ?? "(NoNamespace)";
                return;
            }

            throw new System.Exception($"Can not resolve {nameof(ITypeSymbol)} for Generic argument");

        }

        private void GeneratePartialPartTweener(
            GeneratorExecutionContext context
            , string tweenerName
            , string tweenerNamespace)
        {
            string sourceCode = $@"
using System;
namespace {tweenerNamespace}
{{
    public partial struct {tweenerName}
    {{
        [Unity.Collections.ReadOnly]
        public float DeltaTime;
    }}  
}}
";

            context.AddSource($"{tweenerName}.g.cs", sourceCode);

        }

        private void GenerateCanTweenTag(
            GeneratorExecutionContext context
            , string tweenerName
            , string componentNamespace)
        {
            string sourceCode = $@"
namespace {componentNamespace}
{{
    public struct Can_{tweenerName}_TweenTag : Unity.Entities.IComponentData, Unity.Entities.IEnableableComponent
    {{
    }}
}}
";

            context.AddSource($"Can_{tweenerName}_TweenTag.cs", sourceCode);

        }

        private void GenerateTweenData(
            GeneratorExecutionContext context
            , string tweenerName
            , string targetTypeName
            , string targetNamespace
            , string componentNamespace)
        {
            string fullIdentifier = $"{targetNamespace}.{targetTypeName}";

            string sourceCode = $@"
namespace {componentNamespace}
{{
    public struct {tweenerName}_TweenData : Unity.Entities.IComponentData
    {{
        public float LifeTimeSecond;
        public float BaseSpeed;
        public {fullIdentifier} Target;
    }}
}}
";

            context.AddSource($"{tweenerName}_TweenData.cs", sourceCode);

        }

        private void GenerateTweenSystem(
            GeneratorExecutionContext context
            , string tweenerName
            , string tweenerNamespace
            , string componentTypeName
            , string componentNamespace)
        {
            string componentIdentifier = $"{componentNamespace}.{componentTypeName}";
            string canTweenTagIdentifier = $"{componentNamespace}.Can_{tweenerName}_TweenTag";
            string tweenDataIdentifier = $"{componentNamespace}.{tweenerName}_TweenData";

            string systemName = $"{tweenerName}_TweenSystem";

            string sourceCode = $@"
using Unity.Entities;

namespace TweenLib.Systems
{{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [Unity.Burst.BurstCompile]
    public partial struct {systemName} : ISystem
    {{
        [Unity.Burst.BurstCompile]
        public void OnCreate(ref Unity.Entities.SystemState state)
        {{
            state.RequireForUpdate<{componentIdentifier}>();
            state.RequireForUpdate<{canTweenTagIdentifier}>();
            state.RequireForUpdate<{tweenDataIdentifier}>();
        }}

        [Unity.Burst.BurstCompile]
        public void OnUpdate(ref SystemState state)
        {{
            new TweenJob
            {{
                DeltaTime = SystemAPI.Time.DeltaTime,
            }}.ScheduleParallel();
                
        }}

        [Unity.Burst.BurstCompile]
        public partial struct TweenJob : IJobEntity
        {{
            [Unity.Collections.ReadOnly] public float DeltaTime;

            [Unity.Burst.BurstCompile]
            void Execute(
                EnabledRefRW<{canTweenTagIdentifier}> canTweenTag
                , ref {componentIdentifier} component
                , ref {tweenDataIdentifier} tweenData)
            {{
                var tweener = new {tweenerNamespace}.{tweenerName}
                {{
                    DeltaTime = this.DeltaTime,
                }};

                if (tweener.CanStop(in component, in tweenData.LifeTimeSecond, in tweenData.BaseSpeed, in tweenData.Target))
                {{
                    canTweenTag.ValueRW = false;
                    tweenData.LifeTimeSecond = 0f;
                    return;
                }}

                tweener.Tween(ref component, in tweenData.BaseSpeed, in tweenData.Target);
                tweenData.LifeTimeSecond += this.DeltaTime;

            }}

        }}

    }}

}}
";

            context.AddSource($"{systemName}.g.cs", sourceCode);

        }

    }

}