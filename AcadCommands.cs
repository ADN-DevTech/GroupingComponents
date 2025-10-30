using AcadmAuto;
using AutoCAD;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using GEAuto;
using System;
using System.Runtime.InteropServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
[assembly: CommandClass(typeof(GroupingComponents.AcadCommands))]
[assembly: ExtensionApplication(typeof(GroupingComponents.CommandsRegistrar))]

/// <summary>
/// Before running this code, check that the required libraries are referenced in your project. 

// 1- Autodesk AutoCAD Mechanical 2025 Type Library (C:\Program Files\Common Files\Autodesk Shared\AcadmAuto25.0.tlb)
// 2 - Autodesk GeAuto 2025 Type Library (C:\Program Files\Common Files\Autodesk Shared\GeAuto25.0.tlb)
// 3 - AXDBLib (C:\Program Files\Common Files\Autodesk Shared\acax25enu.tlb)
// 4- AxDb25enu.tlb (C:\Program Files\Common Files\Autodesk Shared\AxDb25enu.tlb)
/// </summary>

class CLSIDUtils
{
   

    [DllImport("oleaut32.dll", PreserveSig = false)]
    static extern void GetActiveObject(
                                              ref Guid rclsid,
                                                  IntPtr pvReserved,
          [MarshalAs(UnmanagedType.IUnknown)] out Object ppunk
        );

    [DllImport("ole32.dll")]
    static extern int CLSIDFromProgID(
        [MarshalAs(UnmanagedType.LPWStr)] string lpszProgID,
                                           out Guid pclsid
     );

    public static object GetActiveObject(string progId)
    {
        Guid clsid;
        CLSIDFromProgID(progId, out clsid);

        object obj;
        GetActiveObject(ref clsid, IntPtr.Zero, out obj);

        return obj;
    }
}



namespace GroupingComponents  // This will be replaced by the user's chosen
                           // namespace
{
/// <summary>
/// This class is used to register the commands in the AutoCAD environment.
/// </summary>
public class CommandsRegistrar : IExtensionApplication
{
    /// <summary>
    /// Initializes the application.
    /// </summary>
    public void Initialize()
    {
        // Register your business logic specific here if needed
        // List all commands
        Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
        ed.WriteMessage("\nAcadCommands plugin loaded successfully!");
    }

    /// <summary>
    /// Cleans up resources when the application is terminated.
    /// </summary>
    public void Terminate()
    {
        // Cleanup code if necessary
    }
}
    /// <summary>
    /// Contains example AutoCAD commands.
    /// </summary>
    public class AcadCommands
    {

        [CommandMethod("CreateComponentGroup")]
        public void CreateComponentGroup()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;


            Editor ed = doc.Editor;


            // COM AutoCAD application (IAcadApplication) - already available to you
            IAcadApplication acadApp = (IAcadApplication)Application.AcadApplication;
            if (acadApp == null)
            {
                ed.WriteMessage("\nUnable to get COM AutoCAD application.");
                return;
            }


            // Mechanical "Acadm" application and 2D structure manager
           // IAcadmApplication acadMapp = CLSIDUtils.GetActiveObject("AcadmAuto.AcadmApplication.25") as IAcadmApplication;
           IAcadmApplication acadMapp = acadApp.GetInterfaceObject("AcadmAuto.AcadmApplication.25") as IAcadmApplication; 

            if (acadMapp == null)
            {
                ed.WriteMessage("\nUnable to get AcadmAuto.AcadmApplication interface.");
                return;
            }


            IMcad2DStructureMgr2 structureMgr2D = acadMapp.ActiveDocument.StructureMgr2D as IMcad2DStructureMgr2;
            if (structureMgr2D == null)
            {
                ed.WriteMessage("\nUnable to get StructureMgr2D.");
                return;
            }


            // Geometry application (GE.Application)
            IGeApplication geApp = acadApp.GetInterfaceObject("GE.Application.25") as IGeApplication;


            // COM AcadDocument (IAcadDocument) for direct COM operations
            IAcadDocument acadDocCom = (IAcadDocument)doc.GetAcadDocument();
            if (acadDocCom == null)
            {
                ed.WriteMessage("\nUnable to get IAcadDocument COM object.");
                return;
            }

            try
            {
                object pickedEntity = null;
                object pickedPoint = null;
                try
                {
                    // Typical COM signature: AcadUtility.GetEntity(out AcadEntity, out VariantPoint, promptString)
                    acadDocCom.Utility.GetEntity(out pickedEntity, out pickedPoint, "Select an entity to add to component group:");
                }
                catch
                {
                    // If GetEntity signature differs, fall back to .NET selection below.
                    pickedEntity = null;
                }
                // If COM GetEntity succeeded, use the returned Acad entity directly
                if (pickedEntity != null)
                {
                    // Check free entity via structure manager
                    bool isFree = false;
                    try
                    {
                    isFree = structureMgr2D.IsFreeEntity((AXDBLib.AcadEntity)pickedEntity);
                    }
                    catch (System.Exception exIsFree)
                    {
                        ed.WriteMessage($"\nIsFreeEntity call failed: {exIsFree.Message}");
                        return;
                    }


                    if (!isFree)
                    {
                        ed.WriteMessage("\nSelected entity already belongs to a component group.");
                        return;
                    }


                    // Ready to create component definition and view
                    CreateComponentAndView(structureMgr2D, geApp, pickedEntity, acadDocCom, ed);
                    return;
                }
            }
            catch (System.Exception)
            {

                throw;
            }
        }

        private void CreateComponentAndView(IMcad2DStructureMgr2 structureMgr2D, IGeApplication geApp, object comEntity, IAcadDocument acadDocCom, Editor ed)
        {
            GePoint startPoint = geApp.Point();
            GePoint endPoint = geApp.Point();
            GeVector compTranslation = geApp.Vector();
            GeMatrix compGeMatrix = geApp.Matrix();

            AcadEntity acadEntity = comEntity as AcadEntity;
            long entId = acadEntity.ObjectID;


            endPoint.Set(10, 10, 0);
            compTranslation.Set(startPoint, endPoint);
            compGeMatrix.SetToTranslation(compTranslation);


            string compName = "MyComponent";


            McadComponentDefinition compDef = null;
            bool hasComp = false;
            try
            {
                hasComp = structureMgr2D.HasComponentDefinition(compName, out compDef);
            }
            catch
            {
               ed.WriteMessage("\nError checking for existing component definition.");
            }


            if (hasComp)
            {
                ed.WriteMessage($"\nComponent '{compName}' already exists.");
                return;
            }


            try
            {
                compDef = structureMgr2D.AddNewComponentDefinition(compName);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nFailed to add component definition: {ex.Message}");
                return;
            }


            McadComponentDefinition parentCompDef = structureMgr2D.RootComponentDefinition;
            McadComponent pCompInstance = parentCompDef.AddComponent(compDef, compGeMatrix);


            McadComponentViewDefinition pNewViewDef = null;
            McadComponentView pNewViewInstance = null;
            object addViews = Type.Missing;
            long[] entityIds = [entId];
            object targetView = Type.Missing;
            object name = "FirstView";
            try
            {
               structureMgr2D.AddNewComponentViewDefinition(
                                    out pNewViewDef, out pNewViewInstance, pCompInstance, compGeMatrix,
                                    addViews, entityIds, targetView, name);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nFailed to create component view definition: {ex.Message}");
                return;
            }


            try
            {
                acadDocCom.Application.ZoomExtents();
            }
            catch { }


            ed.WriteMessage("\nComponent and view created.");
        }
    }
}
// This file is part of the AutoCAD .NET Plugin template.