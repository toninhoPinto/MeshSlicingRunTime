# MeshSlicingRunTime

Slice meshes in runtime in a dynamic way. Very similar to what is implemented in Metal Gear Rising.

The code was divided into multiple implementations, each one with increasingly more features but also more complex and with higher overhead. This way depending if the object is fairly simple to cut there is no need for the more complex computations.

## Table of Content
- [Implementations](#implementations)
- [CutSimpleConvex](#cutsimpleconvex)
- [CutSimpleConcave](#cutsimpleconcave)
- [CutMultiplePartsConcave](#cutmultiplepartsconcave)
- [License](#license)

## Implementations

In the folders Assets/Mesh Slicing/CutMesh/ you will find four scripts, each with a more advanced implementation.

**CutSimpleConvex** > Cut convex mesh. When cut, the new polygon from the intersected zone must also be convex. Always results in two objects.

**CutSimpleConcave** > Cut concave mesh. When cut, the new polygon from the intersected zone must also be convex. Always results in two objects, even if the tris are not all connected.

**CutMultiplePartsConcave** > Cut concave mesh. When cut, the new polygon from the intersected zone must also be convex. Results in n+2 objects in case there are tris that are not connected.

**CutMultiplePartsConcaveAndConcavePolygon** **[NOT YET DONE]**> Cut concave shapes. When cut, the new polygon from the intersected zone can be concave. Results in n+2 objects in case there are tris that are not connected.

**[FUTURE WORK]** Handle hollow objects or objects with holes ??

In the folders Assets/Mesh Slicing/SkeletalCut/ you will find two scripts, each with a more advanced implementation.

**CutSkinnedIntoNonSkinned** > Cut concave skinned mesh. When cut, the new polygon from the intersected zone must also be convex. Results in n+2 objects in case there are tris that are not connected. It results in non-skinned/non animated meshes.

**CutSkinnedIntoSkinned** **[NOT YET DONE]** > Cut concave skinned mesh. The Mesh must be a single object, both in unity gameobject and the triangles must all be continuos. When cut, the new polygon from the intersected zone must also be convex. Results in n+2 objects in case there are tris that are not connected. It results in both skinned and non-skinned meshes.

**[FUTURE WORK]** **CutSkeletonIntoSkinned** - Cut the bones first, should fix the need for continuos meshes.

------------------------------------------------------------------------------------------------------------------------

### CutSimpleConvex

CutSimpleConvex is the simplest implementation.

The first step is to retrieve all the triangles from the mesh of the object you aim to cut.
From there, you go over each of the triangles and do a triangle/plane intersection. If it's not intersected, then it means it can simply be copied into one of the resulting objects from the cut. If it is intersected, then it means it must be sliced into two parts. Each of the sliced parts goes to a different object.

We can observe this in the following image

<p align="center"> 
<img src="http://i.imgur.com/BmGfg67.png">
</p>

In the image, the triangle at the top is not intersected by the plane (represented as a black line), so it's copied 
for the green object. The triangle at the bottom is also not intersected, so it's also copied but this time on another object, so the triangle is colored blue.

Finally the middle triangle IS intersected by the plane. This results in two shapes. A top triangle that uses the original vertex and two new vertexes. This triangle goes to the top green object. The bottom shape is a quadrilateral, so it needs to be split into two triangles. It uses two new vertexes and two old vertexes. These two triangles go to the bottom blue object.

After this you end up with two meshes that together equal the original mesh.
But one thing to remenber is that a mesh is hollow/ just a hull. So when a mesh is cut the location where it is cut ends up with two holes. 

<p align="center"> 
<img src="http://i.imgur.com/GVfGddu.png">
</p>

These holes require two equal polygons to be generated from the new vertexes created when the triangle is intersected by the plane.

For convex polygons (which must happen since the object being cut is convex) it is fairly easy. One can simply use a triangle fan. First one sorts the vertexes clockwise (so that it is easy to create triangles), then average all of the vertexes positions to create a center. With that center all the vertexes can create triangles by connecting 2 at a time to the center.

<p align="center"> 
<img src="http://i.imgur.com/AUO60WA.png">
</p>


These vertexes and tris are then added to both resulting objects.


#### Examples


![](http://i.imgur.com/01veqNg.png)

This is what happens when you cut a non-convex object while going for the naive triangle fan approach.

![](http://i.imgur.com/tafcU7q.png)

------------------------------------------------------------------------------------------------------------------------

### CutSimpleConcave

This version of the code tries to fix the problem displayed in the previous section example where the polygon created is wrong since the mesh is concave. 

The code is overall the same with minor differences and an additional algorithm for the polygon generation.

The first change is how the polygon vertexes are stored. When slicing the triangles in half, instead of saving the two new vertexes, it is actually stored as an line of segment.

<p align="center"> 
<img src="http://i.imgur.com/SRWNDJg.png">
</p>

By having edges that surround the whole mesh in the plane where it was intersected it ends up as a graph like structure.

We can then do a flood-like algorithm that stops when an edge meets itself, meaning that it looped around the mesh. Once there are no more edges to go around it means all the loops have been found. By saving the vertexes of the loops separately, an average for each loop can be easily computed and tris generated for each loop.

#### Examples

You can notice that now, unlike in the previous one, there are multiple SEPARATE polygons where the mesh was cut.

![](http://i.imgur.com/igklB9s.png)

NOTE:While each of the hands, feet and body have their own separate polygons, they do not act differently. The reason is because even though the vertexes are not connected they are indeed the same objects. So while it seems it should be 6 objects it is only 3 objects. 

------------------------------------------------------------------------------------------------------------------------

### CutMultiplePartsConcave

The point of this version is to make mesh parts that are not connected be actual separate objects with individual collisions and physics. 

This version is slightly more complex than the two above. For each triangle that is going to be added to the top or bottom object it is made a new check. The check is made on each of the vertexes of the triangle, if any of those vertexes has been seen before it means a neighbouring triangle has already been added. 

If it has been seen once then add the triangle to the same list of the neighbor triangle. 
If it has been seen multiple times, then add the triangle to the list of one of the neighboring triangles and merge the other lists.
If not, then make a new list. 

In the end, each list ends up with multiple lists of vertexes that are all connected with each other. Each list is a new independent object.

#### Examples

As you can see, everytime the mesh is cut, the arms are created as separate physics objects that fall while the rest of the body does not. 


![](http://i.imgur.com/Yxjn1pC.gif)



![](http://i.imgur.com/AfBQJXt.png)

Like all the above versions, when the polygon is generated it MUST be convex, otherwise it results in wrong triangles.


![](http://i.imgur.com/atJ86tE.png)

## Reading Resources
I might not be following exactly what these resources say since I somehow missed most of them when i was first starting.

[Simple explanation for cutting simple convex shapes](https://gamedevelopment.tutsplus.com/tutorials/how-to-dynamically-slice-a-convex-shape--gamedev-14479 "gamedevelopment.tutsplus")


[Really cool thesis that approaches this problem aswell, with similar solutions](https://github.com/DanniSchou/MeshSplitting/blob/master/Procedural%20Mesh%20Splitting%20-%20by%20Danni%20Schou.pdf "Procedural Mesh Splitting - by Danni Schou")


[A look at Metal Gear Rising in depth](https://simonschreibt.de/gat/metal-gear-rising-slicing/ "Simon Schreibt blog")


[Tiny & Big dev goes over how he did the slicing for his game on a reddit post](https://www.reddit.com/r/gamedev/comments/49vqt5/game_mechanic_dynamic_mesh_cutting_metal_gear/ "Tiny & Big dev")


[Unity forum post about a slicing mesh implementation](https://forum.unity3d.com/threads/real-time-mesh-slicing-demo.357136/ "Unity forum post")



## License

This project is licensed under the MIT License

## Acknowledgments

* Hat tip to anyone who's code was used
* Inspiration
* etc
