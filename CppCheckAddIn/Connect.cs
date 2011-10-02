using System;
using Extensibility;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.CommandBars;
using System.Resources;
using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;

namespace CppCheckAddIn
  {
  /// <summary>The object for implementing an Add-in.</summary>
  /// <seealso class='IDTExtensibility2' />
  public class Connect : IDTExtensibility2, IDTCommandTarget
    {
    /// <summary>Implements the constructor for the Add-in object. Place your initialization code within this method.</summary>
    public Connect()
      {
      }

    /// <summary>Implements the OnConnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being loaded.</summary>
    /// <param term='application'>Root object of the host application.</param>
    /// <param term='connectMode'>Describes how the Add-in is being loaded.</param>
    /// <param term='addInInst'>Object representing this Add-in.</param>
    /// <seealso class='IDTExtensibility2' />
    public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
      {
      _applicationObject = (DTE2)application;
      _addInInstance = (AddIn)addInInst;

      InitializeAddIn();
      
      switch (connectMode)
        {
        case ext_ConnectMode.ext_cm_UISetup:
          break;
        case ext_ConnectMode.ext_cm_Startup:
          break;
        case ext_ConnectMode.ext_cm_AfterStartup:
          mUIHandler.SetupUI();
          break;
        }
      }

    private void InitializeAddIn()
      {
      mOutputHandler = new OutputHandler(_applicationObject);
      mErrorHandler = new ErrorHandler(_applicationObject);
      mUIHandler = new UIHandler(_applicationObject, _addInInstance);
      }

    /// <summary>Implements the OnDisconnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being unloaded.</summary>
    /// <param term='disconnectMode'>Describes how the Add-in is being unloaded.</param>
    /// <param term='custom'>Array of parameters that are host application specific.</param>
    /// <seealso class='IDTExtensibility2' />
    public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
      {
      }

    /// <summary>Implements the OnAddInsUpdate method of the IDTExtensibility2 interface. Receives notification when the collection of Add-ins has changed.</summary>
    /// <param term='custom'>Array of parameters that are host application specific.</param>
    /// <seealso class='IDTExtensibility2' />		
    public void OnAddInsUpdate(ref Array custom)
      {
      }

    /// <summary>Implements the OnStartupComplete method of the IDTExtensibility2 interface. Receives notification that the host application has completed loading.</summary>
    /// <param term='custom'>Array of parameters that are host application specific.</param>
    /// <seealso class='IDTExtensibility2' />
    public void OnStartupComplete(ref Array custom)
      {
      mUIHandler.SetupUI();
      }

    /// <summary>Implements the OnBeginShutdown method of the IDTExtensibility2 interface. Receives notification that the host application is being unloaded.</summary>
    /// <param term='custom'>Array of parameters that are host application specific.</param>
    /// <seealso class='IDTExtensibility2' />
    public void OnBeginShutdown(ref Array custom)
      {
      }

    /// <summary>Implements the QueryStatus method of the IDTCommandTarget interface. This is called when the command's availability is updated</summary>
    /// <param term='commandName'>The name of the command to determine state for.</param>
    /// <param term='neededText'>Text that is needed for the command.</param>
    /// <param term='status'>The state of the command in the user interface.</param>
    /// <param term='commandText'>Text requested by the neededText parameter.</param>
    /// <seealso class='Exec' />
    public void QueryStatus(string commandName, vsCommandStatusTextWanted neededText, ref vsCommandStatus status, ref object commandText)
      {
      if (neededText == vsCommandStatusTextWanted.vsCommandStatusTextWantedNone)
        {
        foreach(string cmdName in mUIHandler.CommandNames)
          if (commandName == "CppCheckAddIn.Connect." + cmdName)
            {
            status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
            return;
            }
        }
      }

    /// <summary>Implements the Exec method of the IDTCommandTarget interface. This is called when the command is invoked.</summary>
    /// <param term='commandName'>The name of the command to execute.</param>
    /// <param term='executeOption'>Describes how the command should be run.</param>
    /// <param term='varIn'>Parameters passed from the caller to the command handler.</param>
    /// <param term='varOut'>Parameters passed from the command handler to the caller.</param>
    /// <param term='handled'>Informs the caller if the command was handled or not.</param>
    /// <seealso class='Exec' />
    public void Exec(string commandName, vsCommandExecOption executeOption, ref object varIn, ref object varOut, ref bool handled)
      {
      handled = false;
      if (executeOption == vsCommandExecOption.vsCommandExecOptionDoDefault)
        {
        }
      }

    private void OutputLineReceivedHandler(object iSender, string iMessage)
      {
      mOutputHandler.OutputMessage("---> " + iMessage);
      }

    private void ErrorLineReceivedHandler(object iSender, string iMessage)
      {
      Regex r = new Regex("^(?<file>.+):::(?<line>.+):::(?<message>.+)$");
      Match m = r.Match(iMessage);

      if (!m.Success || m.Groups.Count != 4)
        return;

      string file = m.Groups["file"].Value;
      string line = m.Groups["line"].Value;
      string message = m.Groups["message"].Value;

      //if (file == null || line == null || message == null)
      //  return;

      mOutputHandler.OutputMessage(file + "(" + line + "): warning: " + message);

      mErrorHandler.AddWarning(file, Int32.Parse(line)-1, message);
      }

    OutputHandler mOutputHandler;
    ErrorHandler mErrorHandler;
    UIHandler mUIHandler;

    private void CreateCommand()
      {
      object[] contextUIGuids = new object[] {};

      _applicationObject.Commands.AddNamedCommand(_addInInstance, "MyCommand", "MyCommand", "MyCommand", true, 59, ref contextUIGuids, (int)vsCommandStatus.vsCommandStatusSupported);
      }

    private void InitializeCommand()
      {
      CommandBarControl myCommandBarControl;
      CommandBar codeWindowCommandBar;
      Command command;
      CommandBars commandBars;

      // Retrieve commands created in the ext_cm_UISetup phase of the OnConnection method
      command = _applicationObject.Commands.Item(_addInInstance.ProgID + ".MyCommand", -1);

      // Retrieve the context menu of code windows
      commandBars = (CommandBars)_applicationObject.CommandBars;
      codeWindowCommandBar = commandBars["Code Window"];

      // Add a popup command bar
      myCommandBarControl = codeWindowCommandBar.Controls.Add(MsoControlType.msoControlPopup, 
         System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing);

      mCommandBarPopup = (CommandBarPopup)myCommandBarControl;

      // Change its caption
      mCommandBarPopup.Caption = "My popup";

      // Add controls to the popup command bar
      mCommandBarControl = (CommandBarControl) command.AddControl(mCommandBarPopup.CommandBar, 
         mCommandBarPopup.Controls.Count + 1);
      }

    private CommandBarPopup mCommandBarPopup;
    private CommandBarControl mCommandBarControl;
    
    private DTE2 _applicationObject;
    private AddIn _addInInstance;
    }
  }