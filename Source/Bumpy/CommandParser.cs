﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bumpy
{
    public sealed class CommandParser
    {
        private readonly IFileUtil _fileUtil;
        private readonly Action<string> _writeLine;

        private CommandType _commandType;
        private int _position;
        private string _formattedNumber;
        private string _version;
        private DirectoryInfo _workingDirectory;
        private FileInfo _configFile;
        private string _profile;

        public CommandParser(IFileUtil fileUtil, Action<string> writeLine)
        {
            _fileUtil = fileUtil;
            _writeLine = writeLine;

            _commandType = CommandType.Help;
            _position = -1;
            _formattedNumber = "-1";
            _version = string.Empty;
            _workingDirectory = new DirectoryInfo(".");
            _configFile = new FileInfo(BumpyConfiguration.ConfigFile);
            _profile = BumpyConfiguration.DefaultProfile;
        }

        public CommandRunner Parse(string[] args)
        {
            try
            {
                ParseCommand(new Queue<string>(args));
            }
            catch (ParserException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ParserException("Invalid arguments. See 'bumpy help'.", e);
            }

            return new CommandRunner(_fileUtil, _writeLine, _commandType, _position, _formattedNumber, _version, _workingDirectory, _configFile, _profile);
        }

        private void ParseCommand(Queue<string> args)
        {
            if (!args.Any())
            {
                return;
            }

            var cmd = args.Dequeue();

            if (!Enum.TryParse(cmd, true, out _commandType))
            {
                throw new ParserException($"Command '{cmd}' not recognized. See 'bumpy help'.");
            }

            if (_commandType == CommandType.Increment || _commandType == CommandType.IncrementOnly)
            {
                _position = Convert.ToInt32(args.Dequeue());
            }
            else if (_commandType == CommandType.Write)
            {
                _version = args.Dequeue();
            }
            else if (_commandType == CommandType.Assign)
            {
                _position = Convert.ToInt32(args.Dequeue());
                _formattedNumber = args.Dequeue();
            }

            var shouldParseOptions = _commandType == CommandType.List
                || _commandType == CommandType.Increment
                || _commandType == CommandType.IncrementOnly
                || _commandType == CommandType.Write
                || _commandType == CommandType.Assign;

            if (!shouldParseOptions)
            {
                if (args.Any())
                {
                    throw new ParserException($"Command '{cmd}' does not accept additional arguments. See 'bumpy help'.");
                }

                return;
            }

            while (args.Any())
            {
                ParseOptions(args);
            }
        }

        private void ParseOptions(Queue<string> args)
        {
            string option = args.Dequeue();

            if (option.Equals("-p"))
            {
                _profile = args.Dequeue();
            }
            else if (option.Equals("-d"))
            {
                _workingDirectory = new DirectoryInfo(args.Dequeue());
            }
            else if (option.Equals("-c"))
            {
                _configFile = new FileInfo(args.Dequeue());
            }
            else
            {
                throw new ParserException($"Option '{option}' not recognized. See 'bumpy help'.");
            }
        }
    }
}
