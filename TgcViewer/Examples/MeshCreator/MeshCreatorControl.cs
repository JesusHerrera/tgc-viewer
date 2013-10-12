﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using TgcViewer.Utils.TgcGeometry;
using Microsoft.DirectX;
using TgcViewer;
using TgcViewer.Utils.Input;
using Examples.MeshCreator.Primitives;
using Examples.MeshCreator.Gizmos;
using Microsoft.DirectX.DirectInput;
using TgcViewer.Utils.Modifiers;
using TgcViewer.Utils.TgcSceneLoader;
using System.IO;
using TgcViewer.Utils._2D;

namespace Examples.MeshCreator
{
    /// <summary>
    /// Control grafico de MeshCreator
    /// </summary>
    public partial class MeshCreatorControl : UserControl
    {
        /// <summary>
        /// Estado general del editor
        /// </summary>
        public enum State
        {
            SelectObject,
            SelectingObject,
            CreatePrimitiveSelected,
            CreatingPrimitve,
            GizmoActivated,
        }

        

        /// <summary>
        /// Colores para crear nuevas primitivas
        /// </summary>
        static readonly Color[] CREATION_COLORS = new Color[]{
            Color.Red,
            Color.Blue,
            Color.Brown,
            Color.Coral,
            Color.DarkCyan,
            Color.Green,
            Color.Purple
        };

        
        TgcMeshCreator creator;
        //int creationColorIdx;
        int primitiveNameCounter;
        TranslateGizmo translateGizmo;
        ScaleGizmo scaleGizmo;
        TgcTextureBrowser textureBrowser;
        string defaultTexturePath;
        string defaultMeshPath;
        TgcMeshBrowser meshBrowser;
        SaveFileDialog exportSceneSaveDialog;
        TgcText2d objectPositionText;
        ObjectBrowser objectBrowser;
        bool fpsCameraEnabled;
        bool ignoreChangeEvents;



        List<EditorPrimitive> meshes;
        /// <summary>
        /// Objetos del escenario
        /// </summary>
        public List<EditorPrimitive> Meshes
        {
            get { return meshes; }
        }

        List<EditorPrimitive> selectionList;
        /// <summary>
        /// Objetos seleccionados
        /// </summary>
        public List<EditorPrimitive> SelectionList
        {
            get { return selectionList; }
        }

        Grid grid;
        /// <summary>
        /// Grid
        /// </summary>
        public Grid Grid
        {
            get { return grid; }
        }

        MeshCreatorCamera camera;
        /// <summary>
        /// Camara
        /// </summary>
        public MeshCreatorCamera Camera
        {
            get { return camera; }
        }

        TgcPickingRay pickingRay;
        /// <summary>
        /// PickingRay
        /// </summary>
        public TgcPickingRay PickingRay
        {
            get { return pickingRay; }
        }

        State currentState;
        /// <summary>
        /// Estado actual
        /// </summary>
        public State CurrentState
        {
            get { return currentState; }
            set { currentState = value; }
        }

        EditorPrimitive creatingPrimitive;
        /// <summary>
        /// Primitiva actual seleccionada para crear
        /// </summary>
        public EditorPrimitive CreatingPrimitive
        {
            get { return creatingPrimitive; }
            set { creatingPrimitive = value; }
        }

        EditorGizmo currentGizmo;
        /// <summary>
        /// Gizmo seleccionado actualmente
        /// </summary>
        public EditorGizmo CurrentGizmo
        {
            get { return currentGizmo; }
            set { currentGizmo = value; }
        }

        SelectionRectangle selectionRectangle;
        /// <summary>
        /// Rectangulo de seleccion
        /// </summary>
        public SelectionRectangle SelectionRectangle
        {
            get { return selectionRectangle; }
        }

        string mediaPath;
        /// <summary>
        /// Archivos de Media propios del editor
        /// </summary>
        public string MediaPath
        {
            get { return mediaPath; }
        }

        /// <summary>
        /// Mostrar AABB de los objetos no seleccionados
        /// </summary>
        public bool ShowObjectsAABB
        {
            get { return checkBoxShowObjectsBoundingBox.Checked; }
        }

        /// <summary>
        /// Hacer Snap to Grid
        /// </summary>
        public bool SnapToGridEnabled
        {
            get { return checkBoxSnapToGrid.Checked; }
        }

        float snapToGridCellSize;
        /// <summary>
        /// Tamaño de celda de Snap to grid
        /// </summary>
        public float SnapToGridCellSize
        {
            get { return snapToGridCellSize; }
        }

        bool popupOpened;
        /// <summary>
        /// True si hay un popup abierto y hay que evitar eventos
        /// </summary>
        public bool PopupOpened
        {
            get { return popupOpened; }
            set { popupOpened = value; }
        }

        /// <summary>
        /// Layer default actual para crear nuevos objetos
        /// </summary>
        public string CurrentLayer
        {
            get { return textBoxCreateCurrentLayer.Text; }
        }


        public MeshCreatorControl(TgcMeshCreator creator)
        {
            InitializeComponent();

            this.creator = creator;
            this.meshes = new List<EditorPrimitive>();
            this.selectionList = new List<EditorPrimitive>();
            this.pickingRay = new TgcPickingRay();
            this.grid = new Grid(this);
            this.selectionRectangle = new SelectionRectangle(this);
            creatingPrimitive = null;
            //creationColorIdx = 0;
            primitiveNameCounter = 0;
            currentGizmo = null;
            mediaPath = GuiController.Instance.ExamplesMediaDir + "MeshCreator\\";
            defaultTexturePath = mediaPath + "Textures\\Madera\\cajaMadera1.jpg";
            checkBoxShowObjectsBoundingBox.Checked = true;
            popupOpened = false;
            fpsCameraEnabled = false;

            //meshBrowser
            defaultMeshPath = mediaPath + "Meshes\\Vegetacion\\Arbusto\\Arbusto-TgcScene.xml";
            meshBrowser = new TgcMeshBrowser();
            meshBrowser.setSelectedMesh(defaultMeshPath);

            //Export scene dialog
            exportSceneSaveDialog = new SaveFileDialog();
            exportSceneSaveDialog.DefaultExt = ".xml";
            exportSceneSaveDialog.Filter = ".XML |*.xml";
            exportSceneSaveDialog.AddExtension = true;
            exportSceneSaveDialog.Title = "Export scene to a -TgcScene.xml file";

            //Camara
            camera = new MeshCreatorCamera();
            camera.Enable = true;
            camera.setCamera(new Vector3(0, 0, 0), 500);
            camera.BaseRotX = -FastMath.PI / 4f;
            GuiController.Instance.CurrentCamera.Enable = false;

            //Gizmos
            translateGizmo = new TranslateGizmo(this);
            scaleGizmo = new ScaleGizmo(this);

            //Tab inicial
            tabControl.SelectedTab = tabControl.TabPages["tabPageCreate"];
            currentState = State.SelectObject;
            radioButtonSelectObject.Checked = true;

            //Tab Create
            textBoxCreateCurrentLayer.Text = "Default";

            //Tab Modify
            textureBrowser = new TgcTextureBrowser();
            textureBrowser.ShowFolders = true;
            textureBrowser.setSelectedImage(defaultTexturePath);
            textureBrowser.AsyncModeEnable = true;
            textureBrowser.OnSelectImage += new TgcTextureBrowser.SelectImageHandler(textureBrowser_OnSelectImage);
            textureBrowser.OnClose += new TgcTextureBrowser.CloseHandler(textureBrowser_OnClose);
            pictureBoxModifyTexture.ImageLocation = defaultTexturePath;
            pictureBoxModifyTexture.Image = MeshCreatorUtils.getImage(defaultTexturePath);
            updateModifyPanel();

            //ObjectPosition Text
            objectPositionText = new TgcText2d();
            objectPositionText.Align = TgcText2d.TextAlign.LEFT;
            objectPositionText.Color = Color.Yellow;
            objectPositionText.Size = new Size(400, 12);
            objectPositionText.Position = new Point(GuiController.Instance.Panel3d.Width - objectPositionText.Size.Width, GuiController.Instance.Panel3d.Height - 20);

            //Snap to grid
            snapToGridCellSize = (float)numericUpDownCellSize.Value;

            //ObjectBrowser
            objectBrowser = new ObjectBrowser(this);
        }

        

        /// <summary>
        /// Ciclo loop del editor
        /// </summary>
        public void render()
        {
            //Hacer update de estado salvo que haya un popup abierto
            if (!popupOpened)
            {
                //Modo camara FPS
                if (fpsCameraEnabled)
                {
                    doFpsCameraMode();
                }
                //Modo normal
                else
                {
                    //Procesar shorcuts de teclado
                    processShortcuts();

                    //Actualizar camara
                    updateCamera();

                    //Maquina de estados
                    switch (currentState)
                    {
                        case State.SelectObject:
                            doSelectObject();
                            break;
                        case State.SelectingObject:
                            doSelectingObject();
                            break;
                        case State.CreatePrimitiveSelected:
                            doCreatePrimitiveSelected();
                            break;
                        case State.CreatingPrimitve:
                            doCreatingPrimitve();
                            break;
                        case State.GizmoActivated:
                            doGizmoActivated();
                            break;
                    }
                }
            }

            



            //Dibujar objetos del escenario (siempre, aunque no haya foco)
            renderObjects();
            
            //Dibujar gizmo (sin Z-Buffer, al final de tod)
            if (currentGizmo != null)
            {
                currentGizmo.render();
            }
        }



        /// <summary>
        /// Procesar shorcuts de teclado
        /// </summary>
        private void processShortcuts()
        {
            //Solo en estados pasivos
            if (currentState == State.SelectObject || currentState == State.CreatePrimitiveSelected || currentState == State.GizmoActivated)
            {
                TgcD3dInput input = GuiController.Instance.D3dInput;

                //Zoom
                if (input.keyPressed(Key.Z))
                {
                    buttonZoomObject_Click(null, null);
                }
                //Hide
                if (input.keyPressed(Key.H))
                {
                    buttonHideSelected_Click(null, null);
                }
                //Select
                else if (input.keyPressed(Key.Q))
                {
                    radioButtonSelectObject.Checked = true;
                }
                //Select all
                else if (input.keyDown(Key.LeftControl) && input.keyPressed(Key.E))
                {
                    buttonSelectAll_Click(null, null);
                }
                //Delete
                else if (input.keyPressed(Key.Delete))
                {
                    buttonDeleteObject_Click(null, null);
                }
                //Translate
                else if (input.keyPressed(Key.W))
                {
                    radioButtonModifySelectAndMove.Checked = true;
                }
                //Scale
                else if (input.keyPressed(Key.R))
                {
                    radioButtonModifySelectAndScale.Checked = true;
                }
                //Clone
                else if (input.keyDown(Key.LeftControl) && input.keyPressed(Key.V))
                {
                    buttonCloneObject_Click(null, null);
                }
                //Create Box
                else if (input.keyPressed(Key.B))
                {
                    radioButtonPrimitive_Box.Checked = true;
                }
                //Create Plane
                else if (input.keyPressed(Key.P))
                {
                    radioButtonPrimitive_PlaneXZ.Checked = true;
                }
                //Import Mesh
                else if (input.keyPressed(Key.M))
                {
                    buttonImportMesh_Click(null, null);
                }
                //Save (Export Scene)
                else if (input.keyDown(Key.LeftControl) && input.keyPressed(Key.S))
                {
                    buttonExportScene_Click(null, null);
                }
                //Help
                else if (input.keyPressed(Key.F1))
                {
                    buttonHelp_Click(null, null);
                }
                //Camara FPS
                else if (input.keyPressed(Key.F))
                {
                    radioButtonFPSCamera.Checked = true;
                }
                //Object browser
                else if (input.keyPressed(Key.O))
                {
                    buttonObjectBrowser_Click(null, null);
                }
                //Top view
                else if (input.keyPressed(Key.T))
                {
                    buttonTopView_Click(null, null);
                }
                //Left view
                else if (input.keyPressed(Key.L))
                {
                    //TODO: no anda
                    //selectionRectangle.setLeftView();
                }
                //Merge selected
                else if (input.keyPressed(Key.G))
                {
                    buttonMergeSelected_Click(null, null);
                }
            }
        }

        


        /// <summary>
        /// Dibujar todos los objetos
        /// </summary>
        private void renderObjects()
        {
            //Objetos opacos
            foreach (EditorPrimitive mesh in meshes)
            {
                if (!mesh.AlphaBlendEnable)
                {
                    if (mesh.Visible)
                    {
                        mesh.render();
                        if (ShowObjectsAABB || mesh.Selected)
                        {
                            mesh.BoundingBox.render();
                        }
                    }
                }
            }

            //Grid
            grid.render();

            //Objeto que se esta construyendo actualmente
            if (currentState == State.CreatingPrimitve)
            {
                creatingPrimitive.render();
            }

            //Recuadro de seleccion
            if (currentState == State.SelectingObject)
            {
                selectionRectangle.render();
            }

            //Objetos transparentes
            foreach (EditorPrimitive mesh in meshes)
            {
                if (mesh.AlphaBlendEnable)
                {
                    if (mesh.Visible)
                    {
                        mesh.render();
                        if (ShowObjectsAABB || mesh.Selected)
                        {
                            mesh.BoundingBox.render();
                        }
                    }
                }
            }

            //Object position Text
            if (selectionList.Count > 0)
            {
                string text = selectionList.Count > 1 ? selectionList[0].Name + " + others" : selectionList[0].Name;
                text += ", Pos: " + TgcParserUtils.printVector3(selectionList[0].Position);
                text += ", Selected " + selectionList.Count;
                objectPositionText.Text = text;
                objectPositionText.render();
            }
        }

        /// <summary>
        /// Actualizar camara segun movimientos
        /// </summary>
        private void updateCamera()
        {
            //Ajustar velocidad de zoom segun distancia a objeto
            Vector3 q;
            if (selectionList.Count > 0)
            {
                q = selectionList[0].BoundingBox.PMin;
            }
            else
            {
                q = Vector3.Empty;
            }
            camera.ZoomFactor = MeshCreatorUtils.getMouseZoomSpeed(this.camera, q);

            camera.updateCamera();
            camera.updateViewMatrix(GuiController.Instance.D3dDevice);
        }


        /// <summary>
        /// Modo camara FPS
        /// </summary>
        private void doFpsCameraMode()
        {
            //No hay actualizar la camara FPS, la actualiza GuiController

            //Detectar si hay que salor de este modo
            TgcD3dInput input = GuiController.Instance.D3dInput;
            if (input.keyPressed(Key.F))
            {
                radioButtonFPSCamera.Checked = false;
            }
        }

        /// <summary>
        /// Estado: seleccionar objetos (estado default)
        /// </summary>
        private void doSelectObject()
        {
            selectionRectangle.doSelectObject();
        }

        /// <summary>
        /// Estado: Cuando se esta arrastrando el mouse para armar el cuadro de seleccion
        /// </summary>
        private void doSelectingObject()
        {
            selectionRectangle.render();
        }

        /// <summary>
        /// Estado: cuando se hizo clic en algun boton de primitiva para crear
        /// </summary>
        private void doCreatePrimitiveSelected()
        {
            TgcD3dInput input = GuiController.Instance.D3dInput;

            //Quitar gizmo actual
            currentGizmo = null;

            //Si hacen clic con el mouse, iniciar creacion de la primitiva
            if (input.buttonDown(TgcD3dInput.MouseButtons.BUTTON_LEFT))
            {
                Vector3 gridPoint = grid.getPicking();
                creatingPrimitive.initCreation(gridPoint);
                currentState = State.CreatingPrimitve;
            }
        }

        /// <summary>
        /// Estado: mientras se esta creando una primitiva
        /// </summary>
        private void doCreatingPrimitve()
        {
            creatingPrimitive.doCreation();
        }

        /// <summary>
        /// Estado: se traslada, rota o escala un objeto
        /// </summary>
        private void doGizmoActivated()
        {
            currentGizmo.update();
        }

        /// <summary>
        /// Agregar mesh creado
        /// </summary>
        public void addMesh(EditorPrimitive mesh)
        {
            this.meshes.Add(mesh);
        }
        

        /*
        /// <summary>
        /// Siguiente color para crear objeto
        /// </summary>
        public Color getCreationColor()
        {
            creationColorIdx = (creationColorIdx + 1) % CREATION_COLORS.Length;
            return CREATION_COLORS[creationColorIdx];
        }
        */

        /// <summary>
        /// Textura para crear un nuevo objeto
        /// </summary>
        /// <returns></returns>
        public string getCreationTexturePath()
        {
            return pictureBoxModifyTexture.ImageLocation;
        }

        /// <summary>
        /// Nombre para crear una nueva primitiva
        /// </summary>
        public string getNewPrimitiveName(string type)
        {
            return type + "_" + primitiveNameCounter++;
        }

        /// <summary>
        /// Eliminar los objetos especificados
        /// </summary>
        public void deleteObjects(List<EditorPrimitive> objectsToDelete)
        {
            foreach (EditorPrimitive p in objectsToDelete)
            {
                if (p.Selected)
                {
                    selectionList.Remove(p);
                }
                meshes.Remove(p);
                p.dispose();
            }

            //Actualizar panel de modifiy
            updateModifyPanel();

            //Quitar gizmo actual
            currentGizmo = null;

            //Pasar a modo seleccion
            currentState = MeshCreatorControl.State.SelectObject;
        }

        /// <summary>
        /// Eliminar todos los objetos seleccionados
        /// </summary>
        public void deleteSelectedObjects()
        {
            foreach (EditorPrimitive p in selectionList)
            {
                meshes.Remove(p);
                p.dispose();
            }

            //Limpiar lista de seleccion
            selectionList.Clear();
            updateModifyPanel();

            //Quitar gizmo actual
            currentGizmo = null;

            //Pasar a modo seleccion
            currentState = MeshCreatorControl.State.SelectObject;
        }

        /// <summary>
        /// Mostrar u ocultar una lista de objetos.
        /// Si estaban seleccionados y se ocultan los quita de la lista de seleccion
        /// </summary>
        public void showHideObjects(List<EditorPrimitive> objects, bool show)
        {
            if (objects.Count > 0)
            {
                foreach (EditorPrimitive p in objects)
                {
                    //Mostrar
                    if (show)
                    {
                        p.Visible = true;
                    }
                    //Ocultar
                    else
                    {
                        p.Visible = false;
                        //Si estaba seleccionado entonces quitar seleccion
                        if (p.Selected)
                        {
                            p.setSelected(false);
                            selectionList.Remove(p);
                        }
                    }
                }

                updateModifyPanel();

                //Quitar gizmo actual
                currentGizmo = null;

                //Pasar a modo seleccion
                currentState = MeshCreatorControl.State.SelectObject;
            }
        }

        /// <summary>
        /// Cargar Tab de Modify cuando hay un objeto seleccionado
        /// </summary>
        public void updateModifyPanel()
        {
            ignoreChangeEvents = true;
            if (selectionList.Count >= 1)
            {
                groupBoxModifyGeneral.Enabled = true;
                groupBoxModifyTexture.Enabled = true;
                groupBoxModifyPosition.Enabled = true;
                groupBoxModifyRotation.Enabled = true;
                groupBoxModifyScale.Enabled = true;
                groupBoxModifyUserProps.Enabled = true;

                //Cargar valores generales
                EditorPrimitive p = selectionList[0];
                EditorPrimitive.ModifyCapabilities caps = p.ModifyCaps;
                textBoxModifyName.Text = p.Name;
                textBoxModifyName.Enabled = selectionList.Count == 1;
                textBoxModifyLayer.Text = p.Layer;


                //Cargar textura
                if (caps.ChangeTexture)
                {
                    numericUpDownModifyTextureNumber.Enabled = true;
                    numericUpDownModifyTextureNumber.Minimum = 1;
                    numericUpDownModifyTextureNumber.Maximum = caps.TextureNumbers;
                    numericUpDownModifyTextureNumber.Value = 1;
                    pictureBoxModifyTexture.Enabled = true;
                    if (pictureBoxModifyTexture.Image != null)
                    {
                        pictureBoxModifyTexture.Image.Dispose();
                    }
                    pictureBoxModifyTexture.Image = Image.FromFile(p.getTexture(0).FilePath);
                    pictureBoxModifyTexture.ImageLocation = p.getTexture(0).FilePath;
                    //textureBrowser.setSelectedImage(p.getTexture(0).FilePath);
                    checkBoxModifyAlphaBlendEnabled.Enabled = true;
                    checkBoxModifyAlphaBlendEnabled.Checked = p.AlphaBlendEnable;
                }
                else
                {
                    numericUpDownModifyTextureNumber.Enabled = true;
                    pictureBoxModifyTexture.Enabled = false;
                    checkBoxModifyAlphaBlendEnabled.Enabled = false;
                }
                
                //Cargar OffsetUV
                if (caps.ChangeOffsetUV)
                {
                    numericUpDownTextureOffsetU.Enabled = true;
                    numericUpDownTextureOffsetV.Enabled = true;
                    numericUpDownTextureOffsetU.Value = (decimal)p.TextureOffset.X;
                    numericUpDownTextureOffsetV.Value = (decimal)p.TextureOffset.Y;
                }
                else
                {
                    numericUpDownTextureOffsetU.Enabled = false;
                    numericUpDownTextureOffsetV.Enabled = false;
                }

                //Cargar TilingUV
                if (caps.ChangeTilingUV)
                {
                    numericUpDownTextureTilingU.Enabled = true;
                    numericUpDownTextureTilingV.Enabled = true;
                    numericUpDownTextureTilingU.Value = (decimal)p.TextureTiling.X;
                    numericUpDownTextureTilingV.Value = (decimal)p.TextureTiling.Y;
                }
                else
                {
                    numericUpDownTextureTilingU.Enabled = false;
                    numericUpDownTextureTilingV.Enabled = false;
                }

                //Cargar Position
                if (caps.ChangePosition)
                {
                    numericUpDownModifyPosX.Enabled = true;
                    numericUpDownModifyPosY.Enabled = true;
                    numericUpDownModifyPosZ.Enabled = true;
                    numericUpDownModifyPosX.Value = (decimal)p.Position.X;
                    numericUpDownModifyPosY.Value = (decimal)p.Position.Y;
                    numericUpDownModifyPosZ.Value = (decimal)p.Position.Z;
                }
                else
                {
                    numericUpDownModifyPosX.Enabled = false;
                    numericUpDownModifyPosY.Enabled = false;
                    numericUpDownModifyPosZ.Enabled = false;
                }

                //Cargar Rotation
                if (caps.ChangeRotation)
                {
                    numericUpDownModifyRotX.Enabled = true;
                    numericUpDownModifyRotY.Enabled = true;
                    numericUpDownModifyRotZ.Enabled = true;
                    numericUpDownModifyRotX.Value = (decimal)FastMath.ToDeg(p.Rotation.X);
                    numericUpDownModifyRotY.Value = (decimal)FastMath.ToDeg(p.Rotation.Y);
                    numericUpDownModifyRotZ.Value = (decimal)FastMath.ToDeg(p.Rotation.Z);
                }
                else
                {
                    numericUpDownModifyRotX.Enabled = false;
                    numericUpDownModifyRotY.Enabled = false;
                    numericUpDownModifyRotZ.Enabled = false;
                }

                //Cargar Scale
                if (caps.ChangeScale)
                {
                    numericUpDownModifyScaleX.Enabled = true;
                    numericUpDownModifyScaleY.Enabled = true;
                    numericUpDownModifyScaleZ.Enabled = true;
                    Vector3 scale = p.Scale;
                    numericUpDownModifyScaleX.Value = (decimal)scale.X * 100;
                    numericUpDownModifyScaleY.Value = (decimal)scale.Y * 100;
                    numericUpDownModifyScaleZ.Value = (decimal)scale.Z * 100;
                }
                else
                {
                    numericUpDownModifyScaleX.Enabled = false;
                    numericUpDownModifyScaleY.Enabled = false;
                    numericUpDownModifyScaleZ.Enabled = false;
                }

                //Cargar userProps
                userInfo.Text = MeshCreatorUtils.getUserPropertiesString(p.UserProperties);
                userInfo.Enabled = selectionList.Count == 1;
            }
            else
            {
                groupBoxModifyGeneral.Enabled = false;
                groupBoxModifyTexture.Enabled = false;
                groupBoxModifyPosition.Enabled = false;
                groupBoxModifyRotation.Enabled = false;
                groupBoxModifyScale.Enabled = false;
                groupBoxModifyUserProps.Enabled = false;
            }

            ignoreChangeEvents = false;
        }

        /// <summary>
        /// Liberar recursos
        /// </summary>
        public void close()
        {
            foreach (EditorPrimitive mesh in meshes)
            {
                mesh.dispose();
            }
            grid.dispose();
            selectionRectangle.dispose();
            if (creatingPrimitive != null)
            {
                selectionRectangle.dispose();
            }
            textureBrowser.Close();
            GuiController.Instance.CurrentCamera.Enable = true;
        }


        #region Eventos generales

        /// <summary>
        /// Desactivar todos los radios
        /// </summary>
        private void untoggleAllRadioButtons(RadioButton exceptRadio)
        {
            if (radioButtonPrimitive_Box != exceptRadio)
            {
                radioButtonPrimitive_Box.Checked = false;
            }
            if (radioButtonPrimitive_PlaneXZ != exceptRadio)
            {
                radioButtonPrimitive_PlaneXZ.Checked = false;
            }
            if (radioButtonPrimitive_PlaneXY != exceptRadio)
            {
                radioButtonPrimitive_PlaneXY.Checked = false;
            }
            if (radioButtonPrimitive_PlaneYZ != exceptRadio)
            {
                radioButtonPrimitive_PlaneYZ.Checked = false;
            }
            if (radioButtonSelectObject != exceptRadio)
            {
                radioButtonSelectObject.Checked = false;
            }
            if (radioButtonModifySelectAndMove != exceptRadio)
            {
                radioButtonModifySelectAndMove.Checked = false;
            }
            if (radioButtonModifySelectAndRotate != exceptRadio)
            {
                radioButtonModifySelectAndRotate.Checked = false;
            }
            if (radioButtonModifySelectAndScale != exceptRadio)
            {
                radioButtonModifySelectAndScale.Checked = false;
            }
        }

        /// <summary>
        /// Clic en "Crear Box"
        /// </summary>
        private void radioButtonPrimitive_Box_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonPrimitive_Box.Checked)
            {
                untoggleAllRadioButtons(radioButtonPrimitive_Box);
                currentState = State.CreatePrimitiveSelected;
                creatingPrimitive = new BoxPrimitive(this);
            }
        }

        /// <summary>
        /// Clic en "Crear Sphere"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void radioButtonPrimitive_Sphere_CheckedChanged(object sender, EventArgs e)
        {

            if (radioButtonPrimitive_Sphere.Checked)
            {
                untoggleAllRadioButtons(radioButtonPrimitive_Sphere);
                currentState = State.CreatePrimitiveSelected;
                creatingPrimitive = new SpherePrimitive(this);
            }
        }
        /// <summary>
        /// Clic en "Crear Plano XZ"
        /// </summary>
        private void radioButtonPrimitive_PlaneXZ_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonPrimitive_PlaneXZ.Checked)
            {
                untoggleAllRadioButtons(radioButtonPrimitive_PlaneXZ);
                currentState = State.CreatePrimitiveSelected;
                creatingPrimitive = new PlaneXZPrimitive(this);
            }
        }

        /// <summary>
        /// Clic en "Crear Plano XY"
        /// </summary>
        private void radioButtonPrimitive_PlaneXY_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonPrimitive_PlaneXY.Checked)
            {
                untoggleAllRadioButtons(radioButtonPrimitive_PlaneXY);
                currentState = State.CreatePrimitiveSelected;
                creatingPrimitive = new PlaneXYPrimitive(this);
            }
        }

        /// <summary>
        /// Clic en "Crear Plano YZ"
        /// </summary>
        private void radioButtonPrimitive_PlaneYZ_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonPrimitive_PlaneYZ.Checked)
            {
                untoggleAllRadioButtons(radioButtonPrimitive_PlaneYZ);
                currentState = State.CreatePrimitiveSelected;
                creatingPrimitive = new PlaneYZPrimitive(this);
            }
        }

        /// <summary>
        /// Clic en "Seleccionar objeto"
        /// </summary>
        private void radioButtonSelectObject_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonSelectObject.Checked)
            {
                untoggleAllRadioButtons(radioButtonSelectObject);
                currentState = State.SelectObject;
                creatingPrimitive = null;
                currentGizmo = null;
            }
        }

        /// <summary>
        /// Clic en "Select all"
        /// </summary>
        private void buttonSelectAll_Click(object sender, EventArgs e)
        {
            selectionRectangle.selectAll();
        }

        /// <summary>
        /// Camiar checkbox de "Snap to grid"
        /// </summary>
        private void checkBoxSnapToGrid_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDownCellSize.Enabled = checkBoxSnapToGrid.Checked;
        }

        /// <summary>
        /// Cambiar tamaño de "Cell size"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void numericUpDownCellSize_ValueChanged(object sender, EventArgs e)
        {
            snapToGridCellSize = (float)numericUpDownCellSize.Value;
        }

        /// <summary>
        /// Clic en "Zoom"
        /// </summary>
        private void buttonZoomObject_Click(object sender, EventArgs e)
        {
            selectionRectangle.zoomObject();
        }

        /// <summary>
        /// Clic en "Hide selected"
        /// </summary>
        private void buttonHideSelected_Click(object sender, EventArgs e)
        {
            if (selectionList.Count > 0)
            {
                List<EditorPrimitive> objectsToHide = new List<EditorPrimitive>(selectionList);
                showHideObjects(objectsToHide, false);
            }
        }

        /// <summary>
        /// Clic en "Unhide all""
        /// </summary>
        private void buttonUnhideAll_Click(object sender, EventArgs e)
        {
            foreach (EditorPrimitive p in meshes)
            {
                p.Visible = true;
            }
        }

        /// <summary>
        /// Clic en "Delete object"
        /// </summary>
        private void buttonDeleteObject_Click(object sender, EventArgs e)
        {
            deleteSelectedObjects();
        }

        
        /// <summary>
        /// Clic en "Clonar seleccion"
        /// </summary>
        private void buttonCloneObject_Click(object sender, EventArgs e)
        {
            if (selectionList.Count > 0)
            {
                //Clonar toda la seleccion
                List<EditorPrimitive> cloneMeshes = new List<EditorPrimitive>();
                foreach (EditorPrimitive p in selectionList)
                {
                    EditorPrimitive pClone = p.clone();
                    cloneMeshes.Add(pClone);
                }

                //Agregar al escenario y seleccionarlas
                selectionRectangle.clearSelection();
                foreach (EditorPrimitive pClone in cloneMeshes)
                {
                    this.meshes.Add(pClone);
                    selectionRectangle.selectObject(pClone);
                }
                currentState = MeshCreatorControl.State.SelectObject;
                selectionRectangle.activateCurrentGizmo();
                updateModifyPanel();
            }
        }

        /// <summary>
        /// Clic en "FPS camera"
        /// </summary>
        private void radioButtonFPSCamera_CheckedChanged(object sender, EventArgs e)
        {
            doClicFPSCamera(radioButtonFPSCamera.Checked);
        }

        /// <summary>
        /// Accion de clic en "FPS camera"
        /// </summary>
        private void doClicFPSCamera(bool enabled)
        {
            if (enabled)
            {
                //Limpiar lista de seleccion
                selectionRectangle.clearSelection();
                updateModifyPanel();

                //Quitar gizmo actual
                currentGizmo = null;

                //Pasar a modo seleccion
                currentState = MeshCreatorControl.State.SelectObject;

                //Activar modo FPS
                fpsCameraEnabled = true;
                GuiController.Instance.FpsCamera.Enable = true;
                GuiController.Instance.FpsCamera.setCamera(camera.getPosition(), camera.getLookAt());
            }
            else
            {
                //Volver al modo normal
                fpsCameraEnabled = false;
                GuiController.Instance.FpsCamera.Enable = false;

                //Acomodar camara de editor para centrar donde quedo la camara FPS
                camera.CameraCenter = GuiController.Instance.FpsCamera.getPosition();
                //camera.CameraDistance = 10;
            }
        }

        /// <summary>
        /// Clic en "Speed de FPS Camera"
        /// </summary>
        private void numericUpDownFPSCameraSpeed_ValueChanged(object sender, EventArgs e)
        {
            //Multiplicar velocidad de camara
            float speed = (float)numericUpDownFPSCameraSpeed.Value;
            GuiController.Instance.FpsCamera.MovementSpeed = TgcFpsCamera.DEFAULT_MOVEMENT_SPEED * speed;
            GuiController.Instance.FpsCamera.JumpSpeed = TgcFpsCamera.DEFAULT_JUMP_SPEED * speed;
        }

        /// <summary>
        /// Clic en "Importar Mesh"
        /// </summary>
        private void buttonImportMesh_Click(object sender, EventArgs e)
        {
            DialogResult r = meshBrowser.ShowDialog();
            if (r == DialogResult.OK)
            {
                try
                {
                    //Cargar scene
                    string path = meshBrowser.SelectedMesh;
                    TgcSceneLoader loader = new TgcSceneLoader();
                    TgcScene scene = loader.loadSceneFromFile(path);

                    //Agregar meshes al escenario y tambien seleccionarlas
                    selectionRectangle.clearSelection();
                    foreach (TgcMesh mesh in scene.Meshes)
                    {
                        MeshPrimitive p = new MeshPrimitive(this, mesh);
                        meshes.Add(p);
                        selectionRectangle.selectObject(p);
                    }
                    currentState = MeshCreatorControl.State.SelectObject;
                    selectionRectangle.activateCurrentGizmo();
                    updateModifyPanel();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Hubo un error al importar el mesh." + "Error: " + ex.Message + " - " + ex.InnerException.Message,
                        "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Clic en "Object browser"
        /// </summary>
        private void buttonObjectBrowser_Click(object sender, EventArgs e)
        {
            objectBrowser.loadObjects("");
            popupOpened = true;
            objectBrowser.Show(this);
        }

        /// <summary>
        /// Clic en "Top view"
        /// </summary>
        private void buttonTopView_Click(object sender, EventArgs e)
        {
            selectionRectangle.setTopView();
        }

        /// <summary>
        /// Clic en "Merge Selected"
        /// </summary>
        private void buttonMergeSelected_Click(object sender, EventArgs e)
        {
            //Que haya mas de uno seleccionado
            if (selectionList.Count > 1)
            {
                //Clonar objetos a mergear
                List<TgcMesh> objectsToMerge = new List<TgcMesh>();
                foreach (EditorPrimitive p in selectionList)
                {
                    TgcMesh m = p.createMeshToExport();
                    objectsToMerge.Add(m);
                }

                //Eliminar los originales
                deleteSelectedObjects();

                //Hacer merge
                TgcSceneExporter exporter = new TgcSceneExporter();
                TgcMesh mergedMesh = exporter.mergeMeshes(objectsToMerge);

                //Hacer dispose de los objetos clonados para mergear
                foreach (TgcMesh m in objectsToMerge)
                {
                    m.dispose();
                }

                //Agregar nuevo mesh al escenario y seleccionarlo
                EditorPrimitive pMerged = new MeshPrimitive(this, mergedMesh);
                this.meshes.Add(pMerged);
                selectionRectangle.selectObject(pMerged);
                currentState = MeshCreatorControl.State.SelectObject;
                selectionRectangle.activateCurrentGizmo();
                updateModifyPanel();
            }
        }

        /// <summary>
        /// Clic en "Exportar escena"
        /// </summary>
        private void buttonExportScene_Click(object sender, EventArgs e)
        {
            if (exportSceneSaveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    //Obtener nombre y directorio
                    FileInfo fInfo = new FileInfo(exportSceneSaveDialog.FileName);
                    string sceneName = fInfo.Name.Split('.')[0];
                    sceneName = sceneName.Replace("-TgcScene", "");
                    string saveDir = fInfo.DirectoryName;

                    //Convertir todos los objetos del escenario a un TgcMesh y agregarlos a la escena a exportar
                    TgcScene exportScene = new TgcScene(sceneName, saveDir);
                    foreach (EditorPrimitive p in meshes)
                    {
                        TgcMesh m = p.createMeshToExport();
                        exportScene.Meshes.Add(m);
                    }

                    //Exportar escena
                    TgcSceneExporter exporter = new TgcSceneExporter();
                    TgcSceneExporter.ExportResult result;
                    if (checkBoxAttachExport.Checked)
                    {
                        result = exporter.exportAndAppendSceneToXml(exportScene, saveDir);
                    }
                    else
                    {
                        result = exporter.exportSceneToXml(exportScene, saveDir);
                    }

                    //Hacer dispose de los objetos clonados para exportar
                    exportScene.disposeAll();
                    exportScene = null;

                    //Mostrar resultado
                    if (result.result)
                    {
                        if (result.secondaryErrors)
                        {
                            MessageBox.Show(this, "La escena se exportó OK pero hubo errores secundarios. " + result.listErrors(), "Export Scene", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show(this, "Scene exported OK.", "Export Scene", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else 
                    {
                        MessageBox.Show(this, "Ocurrieron errores al intentar exportar la escena. " + result.listErrors(),
                            "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }


                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Hubo un error al intenar exportar la escena. Puede ocurrir que esté intentando reemplazar el mismo archivo de escena que tiene abierto ahora. Los archivos de Textura por ejemplo no pueden ser reemplazados si se están utilizando dentro del editor. En ese caso debera guardar en uno nuevo. "
                        + "Error: " + ex.Message + " - " + ex.InnerException.Message,
                        "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                
            }
        }

        /// <summary>
        /// Clic en "Help"
        /// </summary>
        private void buttonHelp_Click(object sender, EventArgs e)
        {
            //Abrir PDF
            System.Diagnostics.Process.Start(GuiController.Instance.ExamplesDir + "\\MeshCreator\\Guia MeshCreator.pdf");
        }

        #endregion

    



        #region Eventos de Modify


        /// <summary>
        /// Clic en "Seleccionar y Mover"
        /// </summary>
        private void radioButtonModifySelectAndMove_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonModifySelectAndMove.Checked)
            {
                untoggleAllRadioButtons(radioButtonModifySelectAndMove);
                currentState = State.SelectObject;
                creatingPrimitive = null;
                currentGizmo = translateGizmo;

                //Activar gizmo si hay seleccion
                if (selectionList.Count > 0)
                {
                    selectionRectangle.activateCurrentGizmo();
                }
            }
        }

        private void radioButtonModifySelectAndRotate_CheckedChanged(object sender, EventArgs e)
        {
            //TODO: implementar gizmo de rotacion
        }

        /// <summary>
        /// Clic en "Seleccionar y Escalar"
        /// </summary>
        private void radioButtonModifySelectAndScale_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonModifySelectAndScale.Checked)
            {
                untoggleAllRadioButtons(radioButtonModifySelectAndScale);
                currentState = State.SelectObject;
                creatingPrimitive = null;
                currentGizmo = scaleGizmo;

                //Activar gizmo si hay seleccion
                if (selectionList.Count > 0)
                {
                    selectionRectangle.activateCurrentGizmo();
                }
            }
        }

        /// <summary>
        /// Cambiar nombre de mesh
        /// </summary>
        private void textBoxModifyName_Leave(object sender, EventArgs e)
        {
            if (ignoreChangeEvents) return;
            selectionList[0].Name = textBoxModifyName.Text;
        }

        /// <summary>
        /// Cambiar nombre de layer
        /// </summary>
        private void textBoxModifyLayer_Leave(object sender, EventArgs e)
        {
            foreach (EditorPrimitive p in selectionList)
            {
                p.Layer = textBoxModifyLayer.Text;
            }
            
        }

        /// <summary>
        /// Elegir otro numero de textura para editar
        /// </summary>
        private void numericUpDownModifyTextureNumber_ValueChanged(object sender, EventArgs e)
        {
            if (ignoreChangeEvents) return;
            int n = (int)numericUpDownModifyTextureNumber.Value - 1;

            //Cambiar imagen del pictureBox
            Image img = MeshCreatorUtils.getImage(selectionList[0].getTexture(n).FilePath);
            Image lastImage = pictureBoxModifyTexture.Image;
            pictureBoxModifyTexture.Image = img;
            pictureBoxModifyTexture.ImageLocation = selectionList[0].getTexture(n).FilePath;
            lastImage.Dispose();
        }

        /// <summary>
        /// Hacer clic en el recuadro de textura para cambiarla
        /// </summary>
        private void pictureBoxModifyTexture_Click(object sender, EventArgs e)
        {
            popupOpened = true;

            int n = (int)numericUpDownModifyTextureNumber.Value - 1;
            textureBrowser.setSelectedImage(selectionList[0].getTexture(n).FilePath);

            textureBrowser.Show(this);
        }

        /// <summary>
        /// Cuando se selecciona una imagen en el textureBrowser
        /// </summary>
        public void textureBrowser_OnSelectImage(TgcTextureBrowser textureBrowser)
        {
            if (ignoreChangeEvents) return;

            //Cambiar la textura si es distinta a la que tenia el mesh
            int n = (int)numericUpDownModifyTextureNumber.Value - 1;
            if (textureBrowser.SelectedImage != selectionList[0].getTexture(n).FilePath)
            {
                Image img = MeshCreatorUtils.getImage(textureBrowser.SelectedImage);
                Image lastImage = pictureBoxModifyTexture.Image;
                pictureBoxModifyTexture.Image = img;
                pictureBoxModifyTexture.ImageLocation = textureBrowser.SelectedImage;
                lastImage.Dispose();
                foreach (EditorPrimitive p in selectionList)
                {
                    p.setTexture(TgcTexture.createTexture(pictureBoxModifyTexture.ImageLocation), n);
                }
                
            }
        }

        /// <summary>
        /// Cuando se cierra el textureBrowser
        /// </summary>
        public void textureBrowser_OnClose(TgcTextureBrowser textureBrowser)
        {
            popupOpened = false;
        }

        /// <summary>
        /// Cambiar Alpha Blend
        /// </summary>
        private void checkBoxModifyAlphaBlendEnabled_CheckedChanged(object sender, EventArgs e)
        {
            if (ignoreChangeEvents) return;

            foreach (EditorPrimitive p in selectionList)
            {
                p.AlphaBlendEnable = checkBoxModifyAlphaBlendEnabled.Checked;
            }
        }

        /// <summary>
        /// Cambiar offset U
        /// </summary>
        private void numericUpDownTextureOffsetU_ValueChanged(object sender, EventArgs e)
        {
            if (ignoreChangeEvents) return;

            float value = (float)numericUpDownTextureOffsetU.Value;
            foreach (EditorPrimitive p in selectionList)
            {
                p.TextureOffset = new Vector2(value, p.TextureOffset.Y);
            }
        }

        /// <summary>
        /// Cambiar offset V
        /// </summary>
        private void numericUpDownTextureOffsetV_ValueChanged(object sender, EventArgs e)
        {
            if (ignoreChangeEvents) return;

            float value = (float)numericUpDownTextureOffsetV.Value;
            foreach (EditorPrimitive p in selectionList)
            {
                p.TextureOffset = new Vector2(p.TextureOffset.X, value);
            }
        }

        /// <summary>
        /// Cambiar tile U
        /// </summary>
        private void numericUpDownTextureTilingU_ValueChanged(object sender, EventArgs e)
        {
            if (ignoreChangeEvents) return;

            float value = (float)numericUpDownTextureTilingU.Value;
            foreach (EditorPrimitive p in selectionList)
            {
                p.TextureTiling = new Vector2(value, p.TextureTiling.Y);
            }
        }


        /// <summary>
        /// Cambiar tile V
        /// </summary>
        private void numericUpDownTextureTilingV_ValueChanged(object sender, EventArgs e)
        {
            if (ignoreChangeEvents) return;

            float value = (float)numericUpDownTextureTilingV.Value;
            foreach (EditorPrimitive p in selectionList)
            {
                p.TextureTiling = new Vector2(p.TextureTiling.X, value);
            }
        }


        /// <summary>
        /// Cambiar posicion en X
        /// </summary>
        private void numericUpDownModifyPosX_ValueChanged(object sender, EventArgs e)
        {
            if (ignoreChangeEvents) return;

            EditorPrimitive p = selectionList[0];
            Vector3 oldPos = p.Position;
            p.Position = new Vector3((float)numericUpDownModifyPosX.Value, oldPos.Y, oldPos.Z);
            Vector3 movement = p.Position - oldPos;
            if (currentGizmo != null)
            {
                currentGizmo.move(p, movement);
            }
            for (int i = 1; i < selectionList.Count; i++)
            {
                selectionList[i].move(movement);
            }
        }

        /// <summary>
        /// Cambiar posicion en Y
        /// </summary>
        private void numericUpDownModifyPosY_ValueChanged(object sender, EventArgs e)
        {
            if (ignoreChangeEvents) return;

            EditorPrimitive p = selectionList[0];
            Vector3 oldPos = p.Position;
            p.Position = new Vector3(oldPos.X, (float)numericUpDownModifyPosY.Value, oldPos.Z);
            Vector3 movement = p.Position - oldPos;
            if (currentGizmo != null)
            {
                currentGizmo.move(p, movement);
            }
            for (int i = 1; i < selectionList.Count; i++)
            {
                selectionList[i].move(movement);
            }
        }

        /// <summary>
        /// Cambiar posicion en Z
        /// </summary>
        private void numericUpDownModifyPosZ_ValueChanged(object sender, EventArgs e)
        {
            if (ignoreChangeEvents) return;

            EditorPrimitive p = selectionList[0];
            Vector3 oldPos = p.Position;
            p.Position = new Vector3(oldPos.X, oldPos.Y, (float)numericUpDownModifyPosZ.Value);
            Vector3 movement = p.Position - oldPos;
            if (currentGizmo != null)
            {
                currentGizmo.move(p, movement);
            }
            for (int i = 1; i < selectionList.Count; i++)
            {
                selectionList[i].move(movement);
            }
        }

        /// <summary>
        /// Cambiar rotacion en X
        /// </summary>
        private void numericUpDownModifyRotX_ValueChanged(object sender, EventArgs e)
        {
            if (ignoreChangeEvents) return;

            float rot = FastMath.ToRad((float)numericUpDownModifyRotY.Value);
            foreach (EditorPrimitive p in selectionList)
            {
                p.Rotation = new Vector3(rot, p.Rotation.Y, p.Rotation.Z);
            }
        }

        /// <summary>
        /// Cambiar rotacion en Y
        /// </summary>
        private void numericUpDownModifyRotY_ValueChanged(object sender, EventArgs e)
        {
            if (ignoreChangeEvents) return;

            float rot = FastMath.ToRad((float)numericUpDownModifyRotY.Value);
            foreach (EditorPrimitive p in selectionList)
            {
                p.Rotation = new Vector3(p.Rotation.X, rot, p.Rotation.Z);
            }
        }

        /// <summary>
        /// Cambiar rotacion en Z
        /// </summary>
        private void numericUpDownModifyRotZ_ValueChanged(object sender, EventArgs e)
        {
            if (ignoreChangeEvents) return;

            float rot = FastMath.ToRad((float)numericUpDownModifyRotY.Value);
            foreach (EditorPrimitive p in selectionList)
            {
                p.Rotation = new Vector3(p.Rotation.X, p.Rotation.Y, rot);
            }
        }

        /// <summary>
        /// Calcular "AABB"
        /// </summary>
        private void buttonModifyRecomputeAABB_Click(object sender, EventArgs e)
        {
            foreach (EditorPrimitive p in selectionList)
            {
                p.updateBoundingBox();
            }
        }

        /// <summary>
        /// Cambiar escala en X
        /// </summary>
        private void numericUpDownModifyScaleX_ValueChanged(object sender, EventArgs e)
        {
            if (ignoreChangeEvents) return;

            EditorPrimitive p = selectionList[0];
            Vector3 old = p.Scale;
            p.Scale = new Vector3((float)numericUpDownModifyScaleX.Value / 100f, p.Scale.Y, p.Scale.Z);
            Vector3 diff = p.Scale - old;
            for (int i = 1; i < selectionList.Count; i++)
            {
                selectionList[i].Scale = selectionList[i].Scale + diff;
            }
        }

        /// <summary>
        /// Cambiar escala en Y
        /// </summary>
        private void numericUpDownModifyScaleY_ValueChanged(object sender, EventArgs e)
        {
            if (ignoreChangeEvents) return;

            EditorPrimitive p = selectionList[0];
            Vector3 old = p.Scale;
            p.Scale = new Vector3(p.Scale.X, (float)numericUpDownModifyScaleY.Value / 100f, p.Scale.Z);
            Vector3 diff = p.Scale - old;
            for (int i = 1; i < selectionList.Count; i++)
            {
                selectionList[i].Scale = selectionList[i].Scale + diff;
            }
        }

        /// <summary>
        /// Cambiar escala en Z
        /// </summary>
        private void numericUpDownModifyScaleZ_ValueChanged(object sender, EventArgs e)
        {
            if (ignoreChangeEvents) return;

            EditorPrimitive p = selectionList[0];
            Vector3 old = p.Scale;
            p.Scale = new Vector3(p.Scale.X, p.Scale.Y, (float)numericUpDownModifyScaleZ.Value / 100f);
            Vector3 diff = p.Scale - old;
            for (int i = 1; i < selectionList.Count; i++)
            {
                selectionList[i].Scale = selectionList[i].Scale + diff;
            }
        }

        /// <summary>
        /// Cambiar user properties
        /// </summary>
        private void userInfo_Leave(object sender, EventArgs e)
        {
            if (ignoreChangeEvents) return;

            EditorPrimitive p = selectionList[0];
            p.UserProperties = MeshCreatorUtils.getUserPropertiesDictionary(userInfo.Text);
        }
        

        #endregion

        

        

        




        

        

        

        

        

        

        












































        
    }
}
