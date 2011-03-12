﻿using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NDesk.Options;

namespace stacktracer
{
    class Program
    {
        private const string regPOI = @"\binterface\b|\bclass\b|\bfunction\b|\breturn\b|[""'/{}]";
        private const string regFun = @"\bfunction\b\s*((?:[gs]et\s+)?\w*)\s*\(";
        private const string regCls = @"class\s+(\w+)[\s{]";
        private const string regStr = @"([""'/]).*?(?<!\\)\1";

        private static Regex rePOI = new Regex(regPOI, RegexOptions.Multiline & RegexOptions.IgnoreCase);
        private static Regex reFun = new Regex(regFun, RegexOptions.Multiline & RegexOptions.IgnoreCase);
        private static Regex reCls = new Regex(regCls, RegexOptions.Multiline & RegexOptions.IgnoreCase);
        private static Regex reStr = new Regex(regStr, RegexOptions.Multiline & RegexOptions.IgnoreCase);

        private static string[] ignoreFiles = new[] {"UncaughtExceptionHandler.as", "Constants.as"};

        private static string path;
        private static bool outputOnly = true;

        private static int functionId;

        private static readonly StringWriter functionMappings = new StringWriter(new StringBuilder());

        class StackTraceItem
        {
            public string Name { get; set; }
            public int Depth { get; set; }
            public int Anon { get; set; }
            public int Id { get; set; }
        }

        static void Main(string[] args)
        {
            var mappingsOutputPath = string.Empty;

            var p = new OptionSet
                    {
                            {"output-only=", v => outputOnly = bool.Parse(v)}, 
                            {"path=", v => path = v},
                            {"mappings-output-path=", v => mappingsOutputPath = v},
                    };

            p.Parse(Environment.GetCommandLineArgs());

            var attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                foreach (var file in Directory.GetFiles(path, "*.as", SearchOption.AllDirectories))
                {
                    string fileName = Path.GetFileName(file);                    
                    if (ignoreFiles.Any(x => x == fileName))
                        continue;

                    ProcessFile(file);
                }
            } else
                ProcessFile(Path.Combine(Environment.CurrentDirectory, path));

            if (mappingsOutputPath != string.Empty)
            {
                Console.Out.WriteLine("Writing mapping to {0}", mappingsOutputPath);
                File.WriteAllText(mappingsOutputPath, functionMappings.ToString());
            }
            else
                Console.Out.WriteLine(functionMappings.ToString());            
        }

        static void ProcessFile(string filePath)
        {
            var body = File.ReadAllText(filePath);

            var stack = new Stack();
            StackTraceItem lastf;
            int depth = 0;
            int retvar = 0;
            string klass = "";
            bool alreadyReturned = false;

            string startingBodyInterest;

            Match match = rePOI.Match(body);

            while (match.Success)
            {
                var poi = match.Groups[0];
                int pos = match.Index;
                var endPos = match.Index + match.Length;

                var bodyCurrentInterest = body.Substring(pos);

                string line;
                switch (poi.Value)
                {
                    // Interfaces
                    case "interface":
                        // We just quit if it's an interface class
                        return;                       
                    // Strings
                    case "\'":
                    case "\"":
                    case "/":
                        var strm = reStr.Match(body, pos);
                        Regex strReg = new Regex(@"[=(,]\s*$");
                        if (strm.Success && body.Substring(pos, 2) == "//")
                            endPos = body.IndexOf("\n", pos) - 1;
                        else if (strm.Success && body.Substring(pos, 2) == "/*")
                            endPos = body.IndexOf("*/", pos) - 1;
                        else if (strm.Success && (poi.Value != "/" || strReg.Match(body, 0, pos).Success))
                            endPos = strm.Index + strm.Length;
                        Console.Out.WriteLine("String:" + strm.Value);
                        break;
                    // Class
                    case "class":
                        klass = reCls.Match(body, pos).Groups[1].Value;
                        Console.Out.WriteLine("Class: " + klass);
                        break;
                    // Function
                    case "function":
                        var fnameMatch = reFun.Match(body, pos);
                        var fname = string.Empty;
                        // Regular functions
                        if (fnameMatch.Groups.Count > 1 && fnameMatch.Groups[1].Value != string.Empty)
                        {
                            fname = klass + "." + fnameMatch.Groups[1].Value;
                            Console.Out.WriteLine("Function: " + fname);
                        }
                        // Anonymous functions
                        else
                        {
                            if (stack.Count > 0)
                            {
                                lastf = (StackTraceItem)stack.Peek();
                                lastf.Anon += 1;
                                fname = lastf.Name + ".anon" + lastf.Anon;
                            } 
                            // Anonymous function set to a variable
                            else                            
                                fname = klass + ".anon";
                            
                            Console.Out.WriteLine("Anonymous Function: " + fname);
                        }

                        stack.Push(new StackTraceItem
                                   {
                                           Name = fname,
                                           Depth = depth,
                                           Anon = 0,
                                           Id = ++functionId
                                   });

                        functionMappings.WriteLine(string.Format("{0},{1}", functionId, fname));

                        var brace = body.IndexOf('{', pos) + 1;
                        line = string.Format("\r\nUncaughtExceptionHandler.enterFunction({0});\r\n", functionId);
                        body = body.Substring(0, brace) + line + body.Substring(brace);
                        depth += 1;                        
                        endPos = brace + line.Length;
                        break;
                    // Opening brackets
                    case "{":
                        depth += 1;
                        break;
                    // Return statements
                    case "return":
                        lastf = (StackTraceItem)stack.Peek();
                        var semicolon = body.IndexOf(';', pos);
                        Regex retReg = new Regex(@"return\s*(.*);", RegexOptions.Singleline);
                        var matchResult = retReg.Match(body, pos, semicolon - pos + 1);
                        if (matchResult.Groups[1].Value == string.Empty)
                        {
                            //RETURN WITHOUT VALUE HERE
                            line = string.Format("{{UncaughtExceptionHandler.exitFunction({0});\r\nreturn;}}\r\n", lastf.Id);
                        }
                        else
                        {
                            retvar += 1;
                            var tmpRetValue = matchResult.Groups[1].Value;
                            //RETURN WITH VALUE HERE
                            line = string.Format("{{var __ret{1}__: * ={0};\r\nUncaughtExceptionHandler.exitFunction({2});\r\nreturn __ret{1}__;}}\r\n", tmpRetValue, retvar, lastf.Id);
                        }

                        body = body.Substring(0, pos) + line + body.Substring(semicolon + 1);
                        endPos = pos + line.Length;
                        alreadyReturned = true;
                        break;
                        // Function end
                    case "}":
                        depth -= 1;
                        if (stack.Count > 0 && ((StackTraceItem)stack.Peek()).Depth == depth)
                        {
                            lastf = (StackTraceItem)stack.Pop();
                            //if (!alreadyReturned) {
                                // Function ended without any returns
                                line = string.Format("UncaughtExceptionHandler.exitFunction({0});\r\n", lastf.Id);
                                body = body.Substring(0, pos) + line + body.Substring(pos);
                                endPos += line.Length;
                            //}
                            alreadyReturned = false;
                        }                        
                        break;
                }

                pos = endPos;

                startingBodyInterest = body.Substring(pos);

                match = rePOI.Match(body, pos);
            }

            if (outputOnly)
                Console.Out.WriteLine(body);
            else
                File.WriteAllText(filePath, body);                           
        }
    }
}
