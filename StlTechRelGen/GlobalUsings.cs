// System
global using System.Collections.ObjectModel;
global using System.Globalization;
global using System.Reflection;
global using System.Text;

// Spectre.Console
global using Spectre.Console;

// CWTools & F# interop
global using CWTools.Common;

global using Microsoft.FSharp.Core;

// Project internals
global using StlTechRelGen.Utils;

global using static StlTechRelGen.Lang;
global using static StlTechRelGen.Utils.LogHelper;

// Type aliases

// CWTools node/value types
global using CWNode = CWTools.Process.Node;
global using CWValue = CWTools.Parser.Types.Value;
global using CWLang = CWTools.Common.Lang;
global using CWRange = CWTools.Utilities.Position.range;

// CWTools game data types
global using SECData = CWTools.Games.ScriptedEffectComputedData;
global using STLGameObject = CWTools.Games.GameObject<
	CWTools.Games.ScriptedEffectComputedData,
	CWTools.Games.STLLookup
>;

// YamlDotNet serialization (used via GlobalInstances.Yaml.*)
global using YamlDeserializer = YamlDotNet.Serialization.Deserializer;
global using YamlSerializerBuilder = YamlDotNet.Serialization.SerializerBuilder;
global using IYamlSerializer = YamlDotNet.Serialization.ISerializer;
