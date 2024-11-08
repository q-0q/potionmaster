using UnityEngine;

namespace DefaultNamespace
{
    public static class Util
    {
        
        public static GameObject GetClosestParentWithComponent<T>(this GameObject gameObject) where T : Component
        {
            // Start with the parent GameObject
            Transform currentTransform = gameObject.transform.parent;

            // Traverse up the parent hierarchy
            while (currentTransform != null)
            {
                // Check if the parent has the specified component
                T component = currentTransform.GetComponent<T>();
                if (component != null)
                {
                    return currentTransform.gameObject;  // Return the parent GameObject with the component
                }

                // Move to the next parent
                currentTransform = currentTransform.parent;
            }

            // Return null if no parent with the specified component was found
            return null;
        }
    }
}