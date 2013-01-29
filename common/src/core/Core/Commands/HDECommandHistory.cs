﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using VVVV.Core.Logging;
using VVVV.Core.Model;
using VVVV.Core.Serialization;

namespace VVVV.Core.Commands
{
	/// <summary>
	/// The HDE implementation of ICommandHistory. Sends the comands to the runtime
	/// </summary>
    public class HDECommandHistory : CommandHistory
	{
        private CommandSender FCommandSender;
        private IIDItem FIdItem;

        public HDECommandHistory(IIDItem idItem)
            : base(idItem.Mapper.Map<Serializer>(), null)
        {
            FCommandSender = new CommandSender(Shell.Instance.CommandLineArguments.RemoteIPs, idItem);
            FIdItem = idItem;
        }

        /// <summary>
        /// Executes a command and adds it to the command history if the command
        /// is undoable and sends the command to the remote vvvv.
        /// </summary>
        /// <param name="command">The command to be executed.</param>
        public override void Insert(Command command)
        {
            //HACK: compound command should handled better
            if (command is CompoundCommand)
            {
                //execute and send all sub commands
                var compound = command as CompoundCommand;
                foreach (var subCom in compound.Commands)
                {
                    var xCommand = FSerializer.Serialize(subCom);
                    base.Insert(subCom);
                    DebugHelpers.CatchAndLog(
                        () => FCommandSender.SendCommandAsync(xCommand),
                        string.Format("ExecutedCommandAsync: {0}", subCom));  
                }

                ////insert compound command into history
                //base.Insert(command, false, true);
                //var xCompCommand = FSerializer.Serialize(command);
                //DebugHelpers.CatchAndLog(
                //        () => FCommandSender.OnlyInsertCommand(xCompCommand.ToString()),
                //        string.Format("InsertedCommandAsync: {0}", xCompCommand));

            }
            else
            {
                var xCommand = FSerializer.Serialize(command);
                base.Insert(command);
                DebugHelpers.CatchAndLog(
                    () => FCommandSender.SendCommandAsync(xCommand),
                    string.Format("SendCommandAsync: {0}", command));
            }
            Debug.WriteLine(string.Format("Executed on ClientCommandHistory for {0}", FIdItem.Name));
        }

        public override void Undo()
        {
            FCommandSender.RemoteUndoAsync();
            base.Undo();
        }

        public override void Redo()
        {
            FCommandSender.RemoteRedoAsync();
            base.Redo();
        }
	}
}
