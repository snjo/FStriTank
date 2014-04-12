using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FStriTank
{
    class FSTTfuelGauge : PartModule
    {
        #region cfg file values
        
        [KSPField]
        public string needleName = "needle1";
        [KSPField]
        public Vector3 needleAxis = Vector3.right; // rotates around the x axis if not defined otherwise in the cfg
        [KSPField]
        public float minAngle = -70f;
        [KSPField]
        public float maxAngle = 180f;

        //each module monitors a separate resource type.
        [KSPField]
        public string resourceName = "Oxidizer";

        #endregion

        #region internal logic values

        private Transform needle;

        // The resource in the part that should be displayed by this module
        private PartResource partResource;

        #endregion

        public override void OnStart(PartModule.StartState state)
        {
            // only run this code when in the hangar or while flying
            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
            {
                needle = part.FindModelTransform(needleName);

                // Get the list of resources in this part
                getPartResource();

                // Turn off all fuel level objects expect the max level one. Will be updated in OnUpdate anyways.
                showFuelLevel(1f);
            }
        }

        public override void OnUpdate()
        {
            // only run this code when in the hangar
            if (HighLogic.LoadedSceneIsFlight)
            {
                // Update the current fuel levels in the part
                updateFuel();
            }
        }

        public void Update()
        {
            // only run this code when in the hangar, cause OnUpdate doesn't run in the hangar
            if (HighLogic.LoadedSceneIsEditor)
            {
                // Update the current fuel levels in the part
                updateFuel();                
            }
        }

        private void showFuelLevel(float fuelLevel)
        {
            if (needle != null)
            {
                needle.localRotation = Quaternion.Euler(needleAxis * Mathf.Lerp(minAngle, maxAngle, fuelLevel));                
            }
        }

        private void updateFuel()
        {
            // Reset the fuel levels in case there's an error in the next step
            float currentFuel = 0f;
            float maxFuel = 0f;

            // In a badly configured part, looking for an amount in a resource that doesn't actually exist would cause endless error messages. This is a bit of a left over from older more advanced code, but it's a handy thing in some cases.
            try
            {
                currentFuel = (float)partResource.amount;
                maxFuel = (float)partResource.maxAmount;
            }
            catch
            {
                // If the resources are invalid, try to fetch them again.
                getPartResource();
            }

            // In case a fuel tank has a max capacity of 0 or something, we want to avoid weird numbers
            float fuelLevel = Mathf.Clamp(currentFuel / maxFuel, 0f, 1f);

            // Update the fuel level  meshes
            showFuelLevel(fuelLevel); 
        }

        private void getPartResource()
        {

            // Loop through the resources in the part's ResourceList.
            for (int i = 0; i < part.Resources.Count; i++)
            {
                // If the name matches the resouce name in the part.cfg use that.
                if (part.Resources[i].resourceName == resourceName)
                {
                    // assign the partResource object in this class so it is referencing the Part's relecant PartResource.
                    partResource = part.Resources[i];
                }
            }
        }
    }
}
