using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FStriTank
{
    class FSTTfuelGauge : PartModule
    {
        #region cfg file values
        
        // the name of the gauge needle gameObject
        [KSPField]
        public string needleName = "needle1";
        // the needle is set to rotation 0,0,0 in Unity, and rotates around a single local axis, defined here.
        [KSPField]
        public Vector3 needleAxis = Vector3.right; // equal to (1,0,0). Rotates around the x axis if not defined otherwise in the cfg
        // The angle of the needle when the tank is empty
        [KSPField]
        public float minAngle = 70f;
        // The angle of the needle when the tank is full
        [KSPField]
        public float maxAngle = -220f;

        //each module monitors a separate resource type.
        [KSPField]
        public string resourceName = "Oxidizer";

        #endregion

        #region internal logic values

        // the transform of the gamObject that will be rotated based on fuel level. Fetched in OnStart
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
            // it's a good idea to make sure the thing you are trying to affect is actually present, and a valid object. In case the findModelTransform failed originally
            if (needle != null)
            {
                // First, it's important that you affect the local rotation of the needle, and not rotate it in world space (unlike in the FSTTfuelLevel module)
                // Rotations are always in Quaternions internally, so you need to convert a Vector 3 (Euler angles) into a Quaternion
                // By specifying min and max angles, and lerping between them, we can use any two angles without worrying about them being positive or negative.
                // Like in this case where min is 70, max is -220. Or they could be the right way around. We could even do more than a 360 deg span, like -180 to 900 for something like a multi hand setup like on a clock.
                needle.localRotation = Quaternion.Euler(needleAxis * Mathf.Lerp(minAngle, maxAngle, fuelLevel)); // neeldeAxis is 1,0,0, so if fuel level is 1, we get (1,0,0) * -220 == (-220,0,0)
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

        public override string GetInfo()
        {
            return "Semi Realistic Sloshing!";
        }
    }
}
