# ProjectileDamageEntity
A projectile Entity for Suri MMORPGKit

This is an alternative to the Missile Damage Entity, based on, that don't use rigidbody or colliders. You can setup it to do arc movement, impact effects, no-impact effect, stick on, normal direction of effect at impact, etc. 

--- How to use ---
1. Create an empty gameobject
2. Add the script ProjectileDamageEntity.cs
Dont add anything more. 
-Configuration-
1. If your game is offline, set the bool of Is Offline to true
2. On HitLayers choose all the layers where the projectile can collide
3. If you don't want your projectile to have an arc, skip this point.
  -Has Gravity: Activate if you want your projectile to have "gravity"
  -Custom Gravity: change how the gravity is applied. If left as Vector3.zero, its going to use Physics.Gravity
  -Use Angle: want to use a predefined starting angle?
  -Angle: define the launch angle
  -Recalculate Speed: This going to overwrite the speed of the weapon and calculate the speed bades on distance. This is really usefull for lockon targets.
4. Prediction Steps: This is the number of times it check for contact each frame. Usually with 6-10 is more than enought. Tested with 200+ speed
5. Effects - Choose:
  -If you want the impact effect to instatiate, choose the option "instantiate" adn drag and drop the prefab on the impactEffect slot
  -If you don't want to instatiate, add the effect as a child, deactivate the effect gameobject and drag and drop to the impactEffect slot
  In this case, i suggest to change the destroy delay so it can be displayed before despawn the projectile.
  -If you don't want to add an impact effect, ignore
6. Do the same for the dissapear effect. This one display if the projectile reach max distance without hitting anything.
7. Save as prefab
8. Add to the weapon

Check the tooltips to understand what do what and have fun. Wish it help anyone doing shooters, because this way is more precise and "always hit"... plus arc is really good for "lock on" targets on a more traditional mmorpg style
