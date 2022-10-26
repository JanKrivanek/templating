// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.TestHelper;
using Microsoft.TemplateEngine.Tests;
using Xunit.Abstractions;

namespace Microsoft.TemplateEngine.Authoring.TemplateVerifier.IntegrationTests
{
    public class ExampleTemplateTest : TestBase
    {
        private readonly ILogger _log;

        public ExampleTemplateTest(ITestOutputHelper log)
        {
            _log = new XunitLoggerProvider(log).CreateLogger("TestRun");
        }

        [Fact]
        public async void VerificationEngineSampleDogfoodTest()
        {
            string workingDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName().Replace(".", string.Empty));
            string templateShortName = "TestAssets.SampleTestTemplate";

            //get the template location
            string templateLocation = Path.Combine(TestTemplatesLocation, "TestTemplate");

            TemplateVerifierOptions options = new TemplateVerifierOptions(templateName: templateShortName)
            {
                TemplateSpecificArgs = new string[] { "--paramB", "true" },
                TemplatePath = templateLocation,
                SnapshotsDirectory = "Snapshots",
                OutputDirectory = workingDir,
                VerifyCommandOutput = true,
                DoNotPrependTemplateNameToScenarioName = true,
                DoNotAppendTemplateArgsToScenarioName = true,
                UniqueFor = UniqueForOption.Architecture,
            }
                .WithCustomScrubbers(
                    ScrubbersDefinition.Empty
                        .AddScrubber(sb => sb.Replace("B is enabled", "*******"))
                        .AddScrubber((path, content) =>
                        {
                            if (path.Replace(Path.DirectorySeparatorChar, '/') == "std-streams/stdout.txt")
                            {
                                content.Replace("SampleTestTemplate", "%TEMPLATE%");
                            }
                        }));

            VerificationEngine engine = new VerificationEngine(_log);
            await engine.Execute(options);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////

        [Fact]
        public async void MyAwesomeTemplate_IsBackCompatible_Basic()
        {
            string templateShortName = "ConditionalParams";
            string testAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location!)!;
            string templateLocation = Path.Combine(testAssemblyLocation, "TestTemplates", "ConditionalParameters");

            TemplateVerifierOptions options = new TemplateVerifierOptions(templateName: templateShortName)
            {
                // Run the specific template scenario
                TemplateSpecificArgs = new[] { "--Platform", "Windows", "--Platform", "WindowsPhone" },
                // With local template files (or locally installed template)
                TemplatePath = templateLocation,
                // Capture the command outputs if needed
                VerifyCommandOutput = true,
                // Filter out (or allow-list) files to check
                VerificationExcludePatterns = new[] { "*.csproj" },
                // Decide on differentiating runs of tests
                UniqueFor = UniqueForOption.Architecture,
            }
            // Scrub files content
            .WithCustomScrubbers(
                ScrubbersDefinition.Empty
                    .AddScrubber(sb => sb.Replace("value of ", "#VAL#"), "cs")
                    .AddScrubber((path, content) => content.Replace("SampleTestTemplate", "%TEMPLATE%")));

            VerificationEngine engine = new VerificationEngine(_log);
            await engine.Execute(options);
        }

        [Theory]
        [InlineData(false, new[] { "--Platform", "Windows", "--Platform", "WindowsPhone" })]
        [InlineData(false, new[] { "--Platform", "android", "--UseGsmLocator", "true", "--AndroidSdkVersion", "v13" })]
        [InlineData(true, new[] { "--Platform", "android", "--UseGsmLocator", "true" })]
        [InlineData(true, new[] { "--Platform", "android", "--Platform", "iOS" })]
        [InlineData(false, new[] { "--Platform", "WindowsPhone", "--Platform", "iOS", "--UseGsmLocator", "true" })]
        public async void MyAwesomeTemplate_IsBackCompatible(bool shouldFail, string[] args)
        {
            string templateShortName = "ConditionalParams";
            string testAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location!)!;
            string templateLocation = Path.Combine(testAssemblyLocation, "TestTemplates", "ConditionalParameters");

            TemplateVerifierOptions options = new TemplateVerifierOptions(templateName: templateShortName)
            {
                // Run the specific template scenario
                TemplateSpecificArgs = args,
                // With local template files (or locally installed template)
                TemplatePath = templateLocation,
                // Capture the command outputs if needed
                VerifyCommandOutput = !shouldFail,
                IsCommandExpectedToFail = shouldFail,
                // Filter out (or allow-list) files to check
                VerificationExcludePatterns = new[] { "*.csproj" },
                // Decide on differentiating runs of tests
                UniqueFor = UniqueForOption.Architecture,
                // Consolidate snapshots of some tests
                DoNotAppendTemplateArgsToScenarioName = shouldFail,
            }
                // Scrub files content
                .WithCustomScrubbers(
                ScrubbersDefinition.Empty
                    .AddScrubber(sb => sb.Replace("value of ", "#VAL#"), "cs")
                    .AddScrubber((path, content) => content.Replace("SampleTestTemplate", "%TEMPLATE%")));

            VerificationEngine engine = new VerificationEngine(_log);
            await engine.Execute(options);
        }

        [Fact]
        public async void MyAwesomeTemplate_IsBackCompatible_CustomCheck()
        {
            string templateShortName = "ConditionalParams";
            string testAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location!)!;
            string templateLocation = Path.Combine(testAssemblyLocation, "TestTemplates", "ConditionalParameters");

            TemplateVerifierOptions options = new TemplateVerifierOptions(templateName: templateShortName)
            {
                // Run the specific template scenario
                TemplateSpecificArgs = new string[] { "--Platform", "iOS", "--Platform", "MacOS" },
                // With local template files (or locally installed template)
                TemplatePath = templateLocation,
                // Capture the command outputs if needed
                // VerifyCommandOutput = true,
                // Filter out (or allow-list) files to check
                VerificationExcludePatterns = new[] { "*.csproj" },
                // Decide on differentiating runs of tests
                UniqueFor = UniqueForOption.Architecture,
            }
            // Scrub files content
            .WithCustomScrubbers(
                ScrubbersDefinition.Empty
                    .AddScrubber(sb => sb.Replace("value of ", "#VAL#"), "cs")
                    .AddScrubber((path, content) => content.Replace("SampleTestTemplate", "%TEMPLATE%")))
            .WithCustomDirectoryVerifier(async (directory, fetcher) =>
            {
                await foreach (var (filePath, scrubbedContent) in fetcher.Value)
                {
                    Path.GetExtension(filePath).Should().BeEquivalentTo(".cs");
                    scrubbedContent.Should().Contain("#VAL#");
                }
            });

            VerificationEngine engine = new VerificationEngine(_log);
            await engine.Execute(options);
        }
    }
}
