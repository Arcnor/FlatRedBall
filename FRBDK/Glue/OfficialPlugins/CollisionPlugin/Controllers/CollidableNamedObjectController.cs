﻿using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;
using OfficialPlugins.CollisionPlugin.Managers;
using OfficialPlugins.CollisionPlugin.ViewModels;
using OfficialPluginsCore.CollisionPlugin.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.CollisionPlugin.Controllers
{
    public class CollidableNamedObjectController
    {
        static CollidableNamedObjectRelationshipViewModel ViewModel;

        public static void RegisterViewModel(CollidableNamedObjectRelationshipViewModel viewModel)
        {
            ViewModel = viewModel;
        }

        public static string FirstCollidableIn(NamedObjectSave collisionRelationship)
        {
            return collisionRelationship.Properties.GetValue<string>(
                nameof(CollisionRelationshipViewModel.FirstCollisionName));
        }

        public static string SecondCollidableIn(NamedObjectSave collisionRelationship)
        {
            return collisionRelationship.Properties.GetValue<string>(
                nameof(CollisionRelationshipViewModel.SecondCollisionName));
        }

        public static void RefreshViewModelTo(IElement container,
            NamedObjectSave thisNamedObject,
            CollidableNamedObjectRelationshipViewModel viewModel)
        {
            viewModel.GlueObject = thisNamedObject;
            viewModel.UpdateFromGlueObject();
            viewModel.CanBePartitioned = CollisionCodeGenerator.CanBePartitioned(thisNamedObject);

            viewModel.CollisionRelationshipsTitle =
                $"{thisNamedObject.InstanceName} Collision Relationships";

            var isSingleEntity = thisNamedObject.IsList == false && thisNamedObject.SourceType == SourceType.Entity;
            var isTileShapeCollection = thisNamedObject.SourceClassType ==
                "FlatRedBall.TileCollisions.TileShapeCollection" ||
                thisNamedObject.SourceClassType == "TileShapeCollection";
            List<NamedObjectSave> collidables;
            
            if(isTileShapeCollection)
            {
                // only against collidables:
                collidables = container.AllNamedObjects
                    .Where(item =>
                    {
                        var entity = CollisionRelationshipViewModelController.GetEntitySaveReferencedBy(item);
                        return entity?.ImplementsICollidable == true;
                    })
                    .ToList();
            }
            else
            {
                collidables = container.AllNamedObjects
                    .Where(item =>
                    {
                        return CollisionRelationshipViewModelController.GetIfCanBeReferencedByRelationship(item);
                    })
                    .ToList();
            }

            if(isSingleEntity)
            {
                // don't let this be against itself
                if(collidables.Contains(thisNamedObject))
                {
                    collidables.Remove(thisNamedObject);
                }
            }

            var relationships = container.AllNamedObjects
                .Where(item =>
                {
                    return item.GetAssetTypeInfo() == AssetTypeInfoManager.Self.CollisionRelationshipAti;
                })
                .ToArray();



            viewModel.NamedObjectPairs.Clear();

            var name1 = thisNamedObject.InstanceName;

            var orderedCollidables = collidables.OrderBy(item => item.InstanceName);

            foreach (var collidable in orderedCollidables)
            {
                var name2 = collidable.InstanceName;

                var pairViewModel = new NamedObjectPairRelationshipViewModel();
                pairViewModel.AddObjectClicked += (not, used) => HandleAddCollisionRelationshipAddClicked(pairViewModel);
                pairViewModel.OtherObjectName = collidable.InstanceName;
                pairViewModel.SelectedNamedObjectName = thisNamedObject.InstanceName;

                var relationshipsForThisPair = relationships
                    .Where(item =>
                    {
                        return (FirstCollidableIn(item) == name1 && SecondCollidableIn(item) == name2) ||
                            (FirstCollidableIn(item) == name2 && SecondCollidableIn(item) == name1);
                    })
                    .ToArray();

                foreach(var relationship in relationshipsForThisPair)
                {
                    var relationshipViewModel = new RelationshipListCellViewModel();
                    relationshipViewModel.OwnerNamedObject = thisNamedObject;
                    relationshipViewModel.OtherNamedObject = collidable;
                    relationshipViewModel.CollisionRelationshipNamedObject = relationship;

                    pairViewModel.Relationships.Add(relationshipViewModel);
                }


                viewModel.NamedObjectPairs.Add(pairViewModel);
            }

        }

        private static void HandleAddCollisionRelationshipAddClicked(NamedObjectPairRelationshipViewModel pairViewModel)
        {
            // Vic asks - why is the selected "second"?
            // If I select the player and have it collide against
            // bullets, I would expect a PlayerVsBullets collision...
            //var firstNosName = pairViewModel.OtherObjectName;
            //var secondNosName = pairViewModel.SelectedNamedObjectName;

            var firstNosName = pairViewModel.SelectedNamedObjectName;
            var secondNosName = pairViewModel.OtherObjectName;

            CreateCollisionRelationshipBetweenObjects(firstNosName, secondNosName);
        }

        public static void CreateCollisionRelationshipBetweenObjects(string firstNosName, string secondNosName)
        {
            var addObjectModel = new AddObjectViewModel();

            addObjectModel.SourceType = FlatRedBall.Glue.SaveClasses.SourceType.FlatRedBallType;
            addObjectModel.SelectedAti =
                AssetTypeInfoManager.Self.CollisionRelationshipAti;
                //"FlatRedBall.Math.Collision.CollisionRelationship";
            addObjectModel.ObjectName = "ToBeRenamed";

            IElement selectedElement = GlueState.Self.CurrentElement;
            var selectedNamedObject = GlueState.Self.CurrentNamedObjectSave;

            var newNos =
                GlueCommands.Self.GluxCommands.AddNewNamedObjectTo(addObjectModel,
                selectedElement, listToAddTo: null);

            newNos.Properties.SetValue(nameof(CollisionRelationshipViewModel.IsAutoNameEnabled), true);

            bool needToInvert = selectedNamedObject.SourceType != SourceType.Entity &&
                selectedNamedObject.IsList == false;

            //if(!needToInvert)
            //{
            //    needToInvert = 
            //}

            if (needToInvert)
            {
                newNos.SetFirstCollidableObjectName(secondNosName);
                newNos.SetSecondCollidableObjectName(firstNosName);
            }
            else
            {
                newNos.SetFirstCollidableObjectName(firstNosName);
                newNos.SetSecondCollidableObjectName(secondNosName);
            }


            CollisionRelationshipViewModelController.TryFixSourceClassType(newNos);

            // this will regenerate and save everything too:
            CollisionRelationshipViewModelController.TryApplyAutoName(
                selectedElement, newNos);


            RefreshViewModelTo(selectedElement, selectedNamedObject, ViewModel);

            CollisionRelationshipViewModelController.TryFixMassesForTileShapeCollisionRelationship(selectedElement, newNos);

            GlueCommands.Self.TreeNodeCommands.RefreshCurrentElementTreeNode();

            GlueState.Self.CurrentNamedObjectSave = newNos;
            GlueCommands.Self.DialogCommands.FocusTab("Collision");
        }
    }
}
