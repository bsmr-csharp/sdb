//
// The MIT License (MIT)
//
// Copyright (c) 2013 Alex Rønne Petersen
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Linq;
using System.Reflection;

namespace Mono.Debugger.Client.Commands
{
    sealed class ConfigCommand : MultiCommand
    {
        sealed class ConfigGetCommand : Command
        {
            public override string[] Names
            {
                get { return new[] { "get" }; }
            }

            public override string Summary
            {
                get { return "Get the value of a configuration element."; }
            }

            public override string Syntax
            {
                get { return "config|cfg get <name>"; }
            }

            public override string Help
            {
                get
                {
                    return "Gets the value of the given configuration element.\n" +
                           "\n" +
                           "This is either the default value or the value in '~/.sdb.cfg'.";
                }
            }

            public override void Process(string args)
            {
                var name = args.Split(' ').Where(x => x != string.Empty).FirstOrDefault();

                if (name == null)
                {
                    Log.Error("No configuration element name given");
                    return;
                }

                var props = typeof(Configuration).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var prop in props)
                {
                    if (prop.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        Log.Info("'{0}' = '{1}'", prop.Name, prop.GetValue(Configuration.Current));
                        return;
                    }
                }

                Log.Error("Configuration element '{0}' not found", name);
            }
        }

        sealed class ConfigListCommand : Command
        {
            public override string[] Names
            {
                get { return new[] { "list" }; }
            }

            public override string Summary
            {
                get { return "List all configuration elements and their values."; }
            }

            public override string Syntax
            {
                get { return "config|cfg list"; }
            }

            public override string Help
            {
                get
                {
                    return "Lists all configuration elements and their values.\n" +
                           "\n" +
                           "These are either the default values or the values in '~/.sdb.cfg'.";
                }
            }

            public override void Process(string args)
            {
                var props = typeof(Configuration).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var prop in props)
                    Log.Info("'{0}' = '{1}'", prop.Name, prop.GetValue(Configuration.Current));
            }
        }

        sealed class ConfigResetCommand : Command
        {
            public override string[] Names
            {
                get { return new[] { "reset" }; }
            }

            public override string Summary
            {
                get { return "Reset configuration values to their defaults."; }
            }

            public override string Syntax
            {
                get { return "config|cfg reset"; }
            }

            public override string Help
            {
                get
                {
                    return "Resets all configuration elements to their default values.\n" +
                           "\n" +
                           "Note that this overwrites '~/.sdb.cfg' too.";
                }
            }

            public override void Process(string args)
            {
                Configuration.Defaults();
                Configuration.Apply();

                Log.Info("All configuration values reset");
            }
        }

        sealed class ConfigSetCommand : Command
        {
            public override string[] Names
            {
                get { return new[] { "set" }; }
            }

            public override string Summary
            {
                get { return "Set the value of a configuration element."; }
            }

            public override string Syntax
            {
                get { return "config|cfg set <name> <value>"; }
            }

            public override string Help
            {
                get
                {
                    return "Sets the value of the given configuration element.\n" +
                           "\n" +
                           "If '~/.sdb.cfg' doesn't exist, this command causes it to be created.";
                }
            }

            public override void Process(string args)
            {
                var name = args.Split(' ').Where(x => x != string.Empty).FirstOrDefault();

                if (name == null)
                {
                    Log.Error("No configuration element name given");
                    return;
                }

                var rest = new string(args.Skip(name.Length).ToArray()).Trim();

                if (rest.Length == 0)
                {
                    Log.Error("No configuration value given");
                    return;
                }

                var props = typeof(Configuration).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var prop in props)
                {
                    if (prop.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var was = prop.GetValue(Configuration.Current);

                        object value = null;

                        try
                        {
                            if (prop.PropertyType == typeof(bool))
                                value = bool.Parse(rest);
                            else if (prop.PropertyType == typeof(int))
                                value = int.Parse(rest);
                            else if (prop.PropertyType == typeof(string))
                                value = rest;
                        }
                        catch (Exception ex)
                        {
                            if (ex is FormatException || ex is OverflowException)
                            {
                                Log.Error("Invalid configuration value");
                                return;
                            }

                            throw;
                        }

                        prop.SetValue(Configuration.Current, value);

                        Configuration.Write();
                        Configuration.Apply();

                        Log.Info("'{0}' = '{1}' (was '{2}')", prop.Name, value, was);

                        return;
                    }
                }

                Log.Error("Configuration element '{0}' not found", name);
            }
        }

        public ConfigCommand()
        {
            AddCommand<ConfigGetCommand>();
            AddCommand<ConfigListCommand>();
            AddCommand<ConfigResetCommand>();
            AddCommand<ConfigSetCommand>();

            Forward<ConfigListCommand>();
        }

        public override string[] Names
        {
            get { return new[] { "config", "cfg" }; }
        }

        public override string Summary
        {
            get { return "Manipulate the debugger configuration."; }
        }

        public override string Help
        {
            get
            {
                return "Manipulates the debugger configuration.\n" +
                       "\n" +
                       "Configuration values are stored in '~/.sdb.cfg' and are loaded on debugger\n" +
                       "startup. The file is only created when a configuration value is set.";
            }
        }
    }
}
