# 📝 AutoCAD Mechanical .NET Add-in: GroupingComponents

This project, `GroupingComponents`, is an AutoCAD Mechanical sample. It provides a custom command for working with 2D Mechanical Structure in AutoCAD Mechanical using C# and .NET 8.0.

---

## 2D Mechanical Structure Sample Code Detailed Explanation

This example demonstrates how to use the AutoCAD Mechanical API to create an object and add it as a component to the mechanical structure, complete with a component view.

### 1. Create a Standard AutoCAD Object

The first step is to create a standard AutoCAD entity. For brevity, we'll create a single circle centered at (0,0,0) with a radius of 10:

```csharp
// Create a circle in ModelSpace
using (var tr = db.TransactionManager.StartTransaction())
{
  var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
  var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
  var center = new Point3d(0,0,0);
  var circle = new Circle(center, Vector3d.ZAxis,10);
  btr.AppendEntity(circle);
  tr.AddNewlyCreatedDBObject(circle, true);
  tr.Commit();
  // Save the ObjectId for later use
  var gObjId = circle.ObjectId;
}
```

### 2. Obtain References to the Mechanical and Geometry APIs

Get the AutoCAD Mechanical application and the 2D structure manager:

```csharp
// Get the Acadm application and structure manager
IAcadApplication acadApp = (IAcadApplication)Application.AcadApplication;
IAcadmApplication acadMapp = acadApp.GetInterfaceObject("AcadmAuto.AcadmApplication.25") as IAcadmApplication;
IMcad2DStructureMgr2 gMgr = acadMapp.ActiveDocument.StructureMgr2D as IMcad2DStructureMgr2;
```

Get the Geometry API root object and related objects:

```csharp
IGeApplication gGeApp = acadApp.GetInterfaceObject("GE.Application.25") as IGeApplication;
GePoint startPoint = gGeApp.Point();
GePoint endPoint = gGeApp.Point();
GeVector compTranslation = gGeApp.Vector();
GeMatrix compGeMatrix = gGeApp.Matrix();

endPoint.Set(10,10,0);
compTranslation.Set(startPoint, endPoint);
compGeMatrix.SetToTranslation(compTranslation);
```

### 3. Check if the Entity is Already Part of a Component View

```csharp
// Get the entity by ObjectId and check if it's free
AcadEntity selectedObj = ... // obtain from ObjectId
gMgr.IsFreeEntity(selectedObj);
```

### 4. Name the Component and View

```csharp
string compName = "MyComponent";
string viewName = "FirstView";
```

### 5. Check for Existing Component Definition

```csharp
McadComponentDefinition compDef = null;
bool hasComp = gMgr.HasComponentDefinition(compName, out compDef);
if (hasComp)
{
  // Component already exists
  return;
}
```

### 6. Create Component Definition and Add to Structure

```csharp
compDef = gMgr.AddNewComponentDefinition(compName);
McadComponentDefinition parentCompDef = gMgr.RootComponentDefinition;
McadComponent comp = parentCompDef.AddComponent(compDef, compGeMatrix);
```

### 7. Add a Component View with Geometry

```csharp
object[] entityIds = new object[] { gObjId }; // Use the ObjectId of the circle
McadComponentViewDefinition compViewDef = null;
McadComponentView compView = null;
McadComponentView targetView = null;
gMgr.AddNewComponentViewDefinition(out compViewDef, out compView, comp, compGeMatrix, Type.Missing, entityIds, targetView, viewName);
```

### 8. Zoom to Extents

```csharp
acadDocCom.Application.ZoomExtents();
```

### 9. Demo

[demo image](demo.png)

---

## Prerequisites

Before running this code, ensure the following type libraries are referenced
Please refer [How to add COM references in .NET projects](https://learn.microsoft.com/en-us/dotnet/framework/interop/importing-a-type-library-as-an-assembly) for more details.
- Autodesk AutoCAD Mechanical 2025 Type Library (`AcadmAuto25.0.tlb`)
- Autodesk GeAuto 2025 Type Library (`GeAuto25.0.tlb`)
- AutoCAD Type libraries AXDBLib (`acax25enu.tlb`)
- AutoCAD Type libraries AxDb25enu (`AxDb25enu.tlb`)

---

## 🛠️ Build Instructions

Follow these steps to build the AutoCAD Add-in:

1. **Restore and Build**: Open your terminal or command prompt and navigate to the project's root directory. Then, execute the following commands:

   ```bash
   dotnet restore
   dotnet build -c Release
   ```

2. **Locate Output**: Upon successful build, the compiled output will be available in the following directory:
   
   ```
   bin\Release\net8.0-windows\
   ```
   
---

Madhukar Moogala (Autodesk)


