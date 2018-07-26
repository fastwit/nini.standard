#region Copyright
//
// Nini Configuration Project.
// Copyright (C) 2006 Brent R. Matzelle.  All rights reserved.
//
// This software is published under the terms of the MIT X11 license, a copy of 
// which has been included with this distribution in the LICENSE.txt file.
// 
#endregion

using System;
using System.IO;
using System.Xml;
using Nini.Config;
using NUnit.Framework;
using Microsoft.Win32;
using nini.windows.Config;

namespace Nini.Test.Config
{
	[TestFixture]
	public class RegistryConfigSourceTests
	{
		#region Tests
		[Test]
		public void GetSingleLevel ()
		{
			RegistryConfigSource source = new RegistryConfigSource ();
			source.AddMapping (Registry.LocalMachine, "Software\\Tests\\NiniTestApp\\Pets");
			
			IConfig config = source.Configs["Pets"];
			Assert.AreEqual ("Pets", config.Name);
			Assert.AreEqual (3, config.GetKeys ().Length);
			Assert.AreEqual (source, config.ConfigSource);
			
			Assert.AreEqual ("Chi-chi", config.Get ("cat"));
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual (5,  config.GetInt ("count"));
		}
		
		[Test]
		public void NonExistantKey ()
		{
			var source = new RegistryConfigSource ();
			Assert.Throws<ArgumentException>(() => source.AddMapping (Registry.LocalMachine, "Software\\Tests\\Does\\NotExist"));
		}

		[Test]
		public void SetAndSaveNormal ()
		{
			var source = new RegistryConfigSource ();
			source.AddMapping (Registry.LocalMachine, "Software\\Tests\\NiniTestApp\\Pets");
			
			var config = source.Configs["Pets"];
			Assert.AreEqual ("Chi-chi", config.Get ("cat"));
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual (5,  config.GetInt ("count"));
			
			config.Set ("cat", "Muffy");
			config.Set ("dog", "Spots");
			config.Set ("DoesNotExist", "SomeValue");
			config.Set ("count", 4);
			
			source.Save ();
			
			source = new RegistryConfigSource ();
			source.AddMapping (Registry.LocalMachine, "Software\\Tests\\NiniTestApp\\Pets");
			
			config = source.Configs["Pets"];
			Assert.AreEqual ("Muffy", config.Get ("cat"));
			Assert.AreEqual ("Spots", config.Get ("dog"));
			Assert.AreEqual ("SomeValue", config.Get ("DoesNotExist"));
			Assert.AreEqual (4,  config.GetInt ("count"));
		}
		
		[Test]
		public void Flattened ()
		{
			var source = new RegistryConfigSource ();
			source.AddMapping (Registry.LocalMachine,
                                "Software\\Tests\\NiniTestApp", 
								RegistryRecurse.Flattened);
			
			var config = source.Configs["NiniTestApp"];
			Assert.AreEqual ("Configuration Library", config.Get ("Description"));
			
			config = source.Configs["Pets"];
			Assert.AreEqual ("Pets", config.Name);
			Assert.AreEqual (3, config.GetKeys ().Length);
			Assert.AreEqual (source, config.ConfigSource);
			
			Assert.AreEqual ("Chi-chi", config.Get ("cat"));
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual (5,  config.GetInt ("count"));
			
			config.Set ("cat", "Muffy");
			config.Set ("dog", "Spots");
			config.Set ("count", 4);
			
			source.Save ();
			
			source = new RegistryConfigSource ();
			source.AddMapping (Registry.LocalMachine, "Software\\Tests\\NiniTestApp\\Pets");
			
			config = source.Configs["Pets"];
			Assert.AreEqual ("Muffy", config.Get ("cat"));
			Assert.AreEqual ("Spots", config.Get ("dog"));
			Assert.AreEqual (4,  config.GetInt ("count"));
		}
		
		[Test]
		public void Namespacing ()
		{
			var source = new RegistryConfigSource ();
			var key = Registry.LocalMachine.OpenSubKey ("Software\\Tests");
			source.AddMapping (key, "NiniTestApp", 
								RegistryRecurse.Namespacing);
			
			var config = source.Configs["NiniTestApp"];
			Assert.AreEqual ("Configuration Library", config.Get ("Description"));
			
			config = source.Configs["NiniTestApp\\Pets"];
			Assert.AreEqual ("NiniTestApp\\Pets", config.Name);
			Assert.AreEqual (3, config.GetKeys ().Length);
			Assert.AreEqual (source, config.ConfigSource);
			
			Assert.AreEqual ("Chi-chi", config.Get ("cat"));
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual (5,  config.GetInt ("count"));
			
			config.Set ("cat", "Muffy");
			config.Set ("dog", "Spots");
			config.Set ("count", 4);
			
			source.Save ();
			
			source = new RegistryConfigSource ();
			key = Registry.LocalMachine.OpenSubKey ("Software\\Tests");
			source.AddMapping (key, "NiniTestApp", 
								RegistryRecurse.Namespacing);
			
			config = source.Configs["NiniTestApp\\Pets"];
			Assert.AreEqual ("Muffy", config.Get ("cat"));
			Assert.AreEqual ("Spots", config.Get ("dog"));
			Assert.AreEqual (4,  config.GetInt ("count"));
		}
		
		[Test]
		public void MergeAndSave ()
		{
			var source = new RegistryConfigSource ();
			source.AddMapping (Registry.LocalMachine, "Software\\Tests\\NiniTestApp\\Pets");
			
			var config = source.Configs["Pets"];
			Assert.AreEqual ("Chi-chi", config.Get ("cat"));
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual (5,  config.GetInt ("count"));

			var writer = new StringWriter ();
			writer.WriteLine ("[Pets]");
			writer.WriteLine ("cat = Becky"); // overwrite
			writer.WriteLine ("lizard = Saurus"); // new
			writer.WriteLine ("[People]");
			writer.WriteLine (" woman = Jane");
			writer.WriteLine (" man = John");
			var iniSource = new IniConfigSource(new StringReader (writer.ToString ()));
			
			source.Merge (iniSource);
			
			config = source.Configs["Pets"];
			Assert.AreEqual (4, config.GetKeys ().Length);
			Assert.AreEqual ("Becky", config.Get ("cat"));
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Saurus", config.Get ("lizard"));
		
			config = source.Configs["People"];
			Assert.AreEqual (2, config.GetKeys ().Length);
			Assert.AreEqual ("Jane", config.Get ("woman"));
			Assert.AreEqual ("John", config.Get ("man"));
			
			config.Set ("woman", "Tara");
			config.Set ("man", "Quentin");
			
			source.Save ();
			
			source = new RegistryConfigSource ();
			source.AddMapping (Registry.LocalMachine, "Software\\Tests\\NiniTestApp\\Pets");
			
			config = source.Configs["Pets"];
			Assert.AreEqual (4, config.GetKeys ().Length);
			Assert.AreEqual ("Becky", config.Get ("cat"));
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual ("Saurus", config.Get ("lizard"));
			
			config = source.Configs["People"];
			Assert.IsNull (config); // you cannot merge new sections
			/*
			Assert.AreEqual (2, config.GetKeys ().Length);
			Assert.AreEqual ("Tara", config.Get ("woman"));
			Assert.AreEqual ("Quentin", config.Get ("man"));
			*/
		}

		[Test]
		public void Reload ()
		{
			var source = new RegistryConfigSource ();
			source.AddMapping (Registry.LocalMachine, "Software\\Tests\\NiniTestApp\\Pets");
			source.Configs["Pets"].Set ("cat", "Muffy");
			source.Save ();

			Assert.AreEqual (3, source.Configs["Pets"].GetKeys ().Length);
			Assert.AreEqual ("Muffy", source.Configs["Pets"].Get ("cat"));

			var newSource = new RegistryConfigSource ();
			newSource.AddMapping (Registry.LocalMachine, "Software\\Tests\\NiniTestApp\\Pets");
			Assert.AreEqual (3, newSource.Configs["Pets"].GetKeys ().Length);
			Assert.AreEqual ("Muffy", newSource.Configs["Pets"].Get ("cat"));

			source = new RegistryConfigSource ();
			source.AddMapping (Registry.LocalMachine, "Software\\Tests\\NiniTestApp\\Pets");
			source.Configs["Pets"].Set ("cat", "Misha");
			source.Save (); // saves new value

			newSource.Reload ();
			Assert.AreEqual (3, newSource.Configs["Pets"].GetKeys ().Length);
			Assert.AreEqual ("Misha", newSource.Configs["Pets"].Get ("cat"));
		}

		[Test]
		public void AddConfig ()
		{
			var source = new RegistryConfigSource ();
			var key = Registry.LocalMachine.OpenSubKey("Software\\Tests\\NiniTestApp", true);

			var config = source.AddConfig ("People", key);
			config.Set ("woman", "Tara");
			config.Set ("man", "Quentin");

			source.Save ();

			source = new RegistryConfigSource ();
			source.AddMapping (Registry.LocalMachine, "Software\\Tests\\NiniTestApp\\People");
			
			Assert.AreEqual (1, source.Configs.Count);
			config = source.Configs["People"];
			Assert.AreEqual (2, config.GetKeys ().Length);
			Assert.AreEqual ("Tara", config.Get ("woman"));
			Assert.AreEqual ("Quentin", config.Get ("man"));
		}

		[Test]
		public void AddConfigDefaultKey ()
		{
			var source = new RegistryConfigSource ();
			source.DefaultKey = 
				Registry.LocalMachine.OpenSubKey("Software\\Tests\\NiniTestApp", true);

			var config = source.AddConfig ("People");
			config.Set ("woman", "Tara");
			config.Set ("man", "Quentin");

			source.Save ();

			source = new RegistryConfigSource ();
			source.AddMapping (Registry.LocalMachine, "Software\\Tests\\NiniTestApp\\People");
			
			Assert.AreEqual (1, source.Configs.Count);
			config = source.Configs["People"];
			Assert.AreEqual (2, config.GetKeys ().Length);
			Assert.AreEqual ("Tara", config.Get ("woman"));
			Assert.AreEqual ("Quentin", config.Get ("man"));
		}

		[Test]
		public void AddConfigNoDefaultKey ()
		{
			var source = new RegistryConfigSource ();
			source.AddMapping (Registry.LocalMachine, "Software\\Tests\\NiniTestApp\\Pets");
			
			Assert.Throws<ApplicationException>(() => source.AddConfig ("People"));
		}

		[Test]
		public void RemoveKey ()
		{
			var source = new RegistryConfigSource ();
			source.AddMapping (Registry.LocalMachine, "Software\\Tests\\NiniTestApp\\Pets");
			
			var config = source.Configs["Pets"];
			Assert.AreEqual ("Pets", config.Name);
			Assert.AreEqual (3, config.GetKeys ().Length);
			Assert.AreEqual (source, config.ConfigSource);

			Assert.AreEqual ("Chi-chi", config.Get ("cat"));
			Assert.AreEqual ("Rover", config.Get ("dog"));
			Assert.AreEqual (5,  config.GetInt ("count"));

			config.Remove ("dog");

			source.Save ();

			source = new RegistryConfigSource ();
			source.AddMapping (Registry.LocalMachine, "Software\\Tests\\NiniTestApp\\Pets");

			config = source.Configs["Pets"];
			Assert.AreEqual ("Pets", config.Name);
			Assert.AreEqual (2, config.GetKeys ().Length);
			Assert.AreEqual (source, config.ConfigSource);

			Assert.AreEqual ("Chi-chi", config.Get ("cat"));
			Assert.IsNull (config.Get ("dog"));
			Assert.AreEqual (5,  config.GetInt ("count"));
		}
		#endregion

		#region Setup/tearDown
		[SetUp]
		public void Setup ()
		{
			var software = Registry.LocalMachine.OpenSubKey ("Software\\Tests", true);
			
			var nini = software.CreateSubKey ("NiniTestApp");
			nini.SetValue ("Description", "Configuration Library");
			nini.Flush ();
			
			var pets = nini.CreateSubKey ("Pets");
			pets.SetValue ("dog", "Rover");
			pets.SetValue ("cat", "Chi-chi");
			pets.SetValue ("count", 5); // set DWORD
			pets.Flush ();
		}
		
		[TearDown]
		public void TearDown ()
		{
			var software = Registry.LocalMachine.OpenSubKey ("Software\\Tests", true);
			software.DeleteSubKeyTree ("NiniTestApp");
		}
		#endregion

		#region Private methods
		#endregion
	}
}
