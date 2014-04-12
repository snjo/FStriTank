using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using UnityEngine;

namespace FStriTank
{
    // This module displays a fuel level by swapping meshes in and out.
    public class FSTTfuelLevel : PartModule
    {
        #region cfg file values

        //Each liquid level is name after the base plus its fill level. ex: liquid1 - liquid10.
        [KSPField]
        public string liquidNameBase = "liquid";
        [KSPField]
        public int liquidLevels = 10;

        //each module monitors a separate resource type.
        [KSPField]
        public string resourceName = "LiquidFuel";

        // The object which hold the liquid. This rotates to match gravity and G-forces
        [KSPField]
        public string liquidRotatorName = "liquidParent";

        #endregion

        #region internal logic values

        // The transform of the object that is rotated to match gravity / G-Forces
        private Transform liquidRotator;

        // A list of all the different objects that represent the different fill levels (or their Transforms actually)
        private List<Transform> liquidLevelTransforms = new List<Transform>();

        // The resource in the part that should be displayed by this module
        private PartResource partResource;

        // Only check the resource level periodically. Not really important in this module, but in other modules that look at all tanks in the vessel, it could help. In seconds.
        private float updateFrequency = 1f;
        // The current countodwn time, in seconds
        private float resourceUpdateCountdown = 1f;        

        //private double fuelLevel = 0f;        
        //private List<PartResource> resourceList = new List<PartResource>();        
        private float currentFuel = 0f;
        private float maxFuel = 0f;

        private Vector3 Gforce = Vector3.zero;
        private Vector3 oldVelocity = Vector3.zero;

        #endregion

        public override void OnStart(PartModule.StartState state)
        {
            // only run this code when in the hangar or while flying
            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
            {

                // Find the transform in the part model which matches the cfg name value.
                liquidRotator = part.FindModelTransform(liquidRotatorName);

                // If that didn't work, give a warning. Missing transforms are checked for in update too to avoid error spam.
                if (liquidRotator == null)
                {
                    // These messages show up in the Alt+F2 / Alt+F12 error logs, and in the more detailed output log file.
                    // Comment them out if they were added for testing purposes, or keep them in if they are needed when creating new parts. Excessive disk writes each update for log messages will lag KSP.
                    Debug.Log("FSTTfuelLevel: Could not find liquid rotator transform " + liquidRotatorName);
                }

                // Fill the list of liquid level objects. Finding Transforms instead of GameObjects, because it is easier.                

                // Make sure the list is empty. Not really needed in this case.
                liquidLevelTransforms.Clear();

                // For loops are better than foreach loops in Unity because of garbage collection. Doesn't really matter here, but as long as it's easy to do, it's a good habit to use for loops.
                for (int i = 1; i <= liquidLevels; i++) // maybe I should have used 0-9...
                {
                    Transform newTransform = part.FindModelTransform(liquidNameBase + i);

                    // if the name is not valid, stop the loop, leaving the list empty, or partially empty.
                    if (newTransform == null) break;
                    liquidLevelTransforms.Add(newTransform);
                }
                Debug.Log("FSTTfuelLevel: Added " + liquidLevelTransforms.Count + " fuel level objects to the list");

                // Get the list of resources in this part
                getPartResource();

                // Turn off all fuel level objects expect the max level one. Will be updated in OnUpdate anyways.
                showFuelLevel(liquidLevels);                                
            }
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                // Looking at velocities seems more stable in the physic's FixedUpdate, than in the Update/OnUpdate, which is used for rendering updates.

                // If the vessel/part is at a stand still, velocities jump around crazily, so disregard those scenarios
                if (part.rigidbody.velocity.magnitude > 0.2f)
                {
                    // The change in part velocity between this physics update and the last will give the current G force vector3 (vessel.geeForce is just an magnitude amount, not a 3D vector, so we can't use that)
                    Gforce = Vector3.Lerp(Gforce, oldVelocity - part.rigidbody.velocity, 0.2f);
                }
                else
                {
                    Gforce = Vector3.zero;
                }
                oldVelocity = part.rigidbody.velocity;
            }
        }

        public override void OnUpdate()
        {
            // only run this code when in the hangar
            if (HighLogic.LoadedSceneIsFlight)
            {
                // Update the current fuel levels in the part
                updateFuel();
                
                // Rotate the liquid so it looks towrds the sky (That's how the model is set up, with z axis up).
                // The model is set up with the Z axis facing the sky, which is what LookRotation likes. Add in a portion of the current G forces for fun.
                liquidRotator.rotation = Quaternion.LookRotation(Vector3.Lerp(vessel.upAxis, -Gforce.normalized, 0.3f)); //Mathf.Clamp(Gforce.magnitude / 10f, 0f, 1f)));
                // Could use the commented out tail section of the above to scale G forces influence on ther liquid by how powerful the change is, not just the direction. Not sure about the scale of the G force delta though. Looks OK as is.
            }
        }

        public void Update()
        {
            // only run this code when in the hangar, cause OnUpdate doesn't run in the hangar
            if (HighLogic.LoadedSceneIsEditor)
            {
                // Update the current fuel levels in the part
                updateFuel();
                // Rotate the liquid, same principle as in OnUpdate, except  in the hangar, there is now vessel or world to check for, but we know Vector3.up is towards the sky.
                liquidRotator.rotation = Quaternion.LookRotation(Vector3.up);
            }
        }

        private void showFuelLevel(float fuelLevel)
        {
            int newLevel = (int)(fuelLevel * 10);
            //Disclaimer: Working with list and array index numbers can be confusing. Expect out of range errors the first time you run it unless you REALLY though it through. Which I didn't.

            newLevel -= 1; // an incoming 0 is empty. Inside this function an empty value is converted to -1, so that the max value matches the index number of the list.

            // Loop through each fuel level object, activating or deactivating it if they match the requested display level
            for (int i = 0; i < liquidLevelTransforms.Count; i++)
            {
                if (i == newLevel)
                {
                    // using setActive should prevent errors if there is no mesh renderer on the game object.
                    liquidLevelTransforms[i].gameObject.SetActive(true);
                }
                else
                {
                    liquidLevelTransforms[i].gameObject.SetActive(false);
                }
            }
        }

        private void updateFuel()
        {            
            // if the countdown has reached zer, check the levels agin, otherwise, decrease the count down
            if (resourceUpdateCountdown <= 0)
            {
                // Reset the fuel levels in case there's an error in the next step
                currentFuel = 0f;
                maxFuel = 0f;
                
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

                // Since the countdown was done, restart it
                resourceUpdateCountdown = updateFrequency;
            }
            else
            {
                // Decrease the countdown. Time.deltaTime gives you the time passed since the last update call, making it good for accurate timed events.
                resourceUpdateCountdown -= Time.deltaTime;
            }
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

            // Another way to do it. Easier to read, but slightly less memory efficient:
            /* 
                foreach (PartResource resource in part.Resources)
                {
                    if (resource.resourceName == resourceName)
                    {
                        resourceList.Add(resource);
                    }
                }                        
             */
        }
    }
}
