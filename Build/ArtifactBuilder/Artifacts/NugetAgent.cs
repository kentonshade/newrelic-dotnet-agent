﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ArtifactBuilder.Artifacts
{
	public class NugetAgent : Artifact
	{
		public NugetAgent(string configuration, string sourceDirectory)
			: base(sourceDirectory, nameof(NugetAgent))
		{
			Configuration = configuration;
		}

		public string Configuration { get; }

		protected override void InternalBuild()
		{
			var rootDirectory = $@"{StagingDirectory}\content\newrelic";
			var frameworkAgentComponents = AgentComponents.GetAgentComponents(AgentType.Framework, Configuration, "x64", SourceDirectory);
			var coreAgentComponents = AgentComponents.GetAgentComponents(AgentType.Core, Configuration, "x64", SourceDirectory);
			frameworkAgentComponents.ValidateComponents();
			coreAgentComponents.ValidateComponents();

			var package = new NugetPackage(StagingDirectory, OutputDirectory);

			frameworkAgentComponents.CopyComponents($@"{package.ContentDirectory}\newrelic");
			Directory.CreateDirectory($@"{rootDirectory}\logs");
			System.IO.File.Create($@"{rootDirectory}\logs\placeholder").Dispose();

			frameworkAgentComponents.CopyComponents($@"{package.GetContentFilesDirectory("any", "net45")}\newrelic");
			Directory.CreateDirectory($@"{StagingDirectory}\contentFiles\any\net45\newrelic\logs");
			System.IO.File.Create($@"{StagingDirectory}\contentFiles\any\net45\newrelic\logs\placeholder").Dispose();

			coreAgentComponents.CopyComponents($@"{package.GetContentFilesDirectory("any", "netstandard2.0")}\newrelic");
			package.CopyToContentFiles(coreAgentComponents.LinuxProfiler, @"any\netstandard2.0\newrelic");
			Directory.CreateDirectory($@"{StagingDirectory}\contentFiles\any\netstandard2.0\newrelic\logs");
			System.IO.File.Create($@"{StagingDirectory}\contentFiles\any\netstandard2.0\newrelic\logs\placeholder").Dispose();

			package.CopyAll(PackageDirectory);

			var newRelicConfigPaths = new[]
			{
				$@"{rootDirectory}\newrelic.config",
				$@"{StagingDirectory}\contentFiles\any\net45\newrelic\newrelic.config",
				$@"{StagingDirectory}\contentFiles\any\netstandard2.0\newrelic\newrelic.config",
			};
			
			foreach (var newRelicConfigPath in newRelicConfigPaths)
			{
				TransformNewRelicConfig(newRelicConfigPath);
			} 

			package.SetVersion(frameworkAgentComponents.Version);

			package.Pack();
		}

		private void TransformNewRelicConfig(string newRelicConfigPath)
		{
			var xml = new System.Xml.XmlDocument();

			// Update the 'newrelic.config' file
			xml.Load(newRelicConfigPath);
			var ns = new System.Xml.XmlNamespaceManager(xml.NameTable);
			ns.AddNamespace("x", "urn:newrelic-config");

			// Remove the 'application' element
			var node = xml.SelectSingleNode("//x:configuration/x:application", ns);
			node.ParentNode.RemoveChild(node);

			// Re-create the 'application' element
			var nodeLog = (System.Xml.XmlElement)xml.SelectSingleNode("//x:configuration/x:log", ns);
			var app = xml.CreateElement("application", "urn:newrelic-config");
			xml.DocumentElement.InsertBefore(app, nodeLog);

			xml.Save(newRelicConfigPath);
		}
	}
}
