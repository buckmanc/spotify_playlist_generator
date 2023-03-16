﻿using spotify_playlist_generator.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace spotify_playlist_generator
{
    internal static class Help
    {
        private static string _ParameterHelp;
        public static string ParameterHelp
        {
            get
            {
                if (_ParameterHelp != null)
                    return _ParameterHelp;

                //TODO format playlist parameter help from definitions

                return "documentation pending";
            }
        }

        private static string _OptionHelp;
        public static string OptionHelp
        {
            get
            {
                if (_OptionHelp != null)
                    return _OptionHelp;

                //TODO create a playlist options parser, define options, and format help for them

                var options = typeof(PlaylistSpec).GetProperties()
                    .Select(prop => new { 
                        OptionName = prop.Name,
                        Description = (prop.GetCustomAttribute(typeof(DescriptionAttribute), true) as DescriptionAttribute)?.Description
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x.Description))
                    .OrderBy(x => x.OptionName)
                    .ToArray();

                var maxNameLen = options.Max(x => x.OptionName.Length);

                _OptionHelp = options
                    .Select(x => x.OptionName.PadRight(maxNameLen + 2) + x.Description)
                    .Join(Environment.NewLine)
                    ;

                return _OptionHelp;
            }
        }

        private static string _ArgumentHelp;
        public static string ArgumentHelp
        {
            get
            {
                if (_ArgumentHelp != null)
                    return _ArgumentHelp;

                string helpText = null;

                using (StringWriter stringWriter = new StringWriter())
                {
                    var existingOut = Console.Out;
                    Console.SetOut(stringWriter);
                    System.CommandLine.DragonFruit.CommandLine.ExecuteAssembly(typeof(AutoGeneratedProgram).Assembly, new string[] { "--help" }, "");
                    helpText = stringWriter.ToString();

                    Console.SetOut(existingOut);
                }

                helpText = helpText
                    .ReplaceLineEndings()
                    .Split(Environment.NewLine)
                    .Where(line => !line.Contains("violence")) // easter eggs are more fun if they're tucked away
                    .Select(line => line.Trim())
                    .Join("\n"); //TODO detect line endings from the file

                _ArgumentHelp = helpText;
                return _ArgumentHelp;
            }
        }

        private static string _TabCompletionArgumentNames;

        public static string TabCompletionArgumentNames
        {
            get 
            {
                if (!string.IsNullOrWhiteSpace(_TabCompletionArgumentNames))
                    return _TabCompletionArgumentNames;

                var helpText = Help.ArgumentHelp;
                var argRegex = new Regex(@"(--[a-z\-]+?) ");

                _TabCompletionArgumentNames = argRegex.Matches(helpText)
                    .Select(m => m.Groups.Values.LastOrDefault()?.Value)
                    .Distinct()
                    .Join(" ");

                return _TabCompletionArgumentNames; 
            }
        }

    }
}
