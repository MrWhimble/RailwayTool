# Railway Maker

FOR MORE DETAILS, GO TO: https://github.com/MrWhimble/RailwayTool/wiki

## Customisation

To change the colors, sizes, and other settings; go to Edit > Preferences > MrWhimble > Railway Maker



## Creating a Track

1. Create a GameObject and add:
        * RailwayManager
        * RailwayMeshManager (if you want to generate a visible mesh)
        * RailwayNetworkBehaviour (if you want to have an object go around the track)

2. Create a new rail path by right-clicking assets and clicking: Create > MrWhimble > Rail Path Data

3. Drag the new rail path into the RailwayManager script

4. Create a new curve by clicking "Create New Curve"

5. build your track:
    Controls:
    (MoveAdd):
        * click on a point to move it
        * shift-click on a point to select it and bring up the position gizmo for more precise movement
        * ctrl-click and drag on a point to create a new point from it (only anchor points)
        * drag an anchor point close to another anchor point to combine them
    (Split):
        * shift-click to split the curve where the line connects to
    (Remove):
        * shift-click on a point to remove it (and curves connecting to it) (only anchor points)
    
    Tips:
        * to set the roll of a point, select it and change the Z-axis in the inspector
        * use the constraints to lock the axis of a point will moving it
        * if a control point is on the wrong side, select it and change "flipped"

### Tracks save whenever the RailwayManager is closed!!!



## Creating a Train

1. Create a GameObject for each point of contact with the track and add:
        * Bogie
    This will follow the track. 
    If the object you want to follow the track only has one point of contact with the track (such as a minecart), still add this script

2. Assign "networkBehaviour" in the Bogie script

3. Create a GameObject for each locomotive/carriage/railcar/wagon and add:
        * Train

4. Position the GameObjects with the Bogie script so they are spaced the same distance they would be on the track

5. Assign the GameObjects with the Bogie script to "startBogie" and "endBogie" in the GameObjects with the Train script

6. Assign the GameObject with the Bogie script to "leader" in the GameObject with the Bogie script after it (so a bogie points to the one before it).

7. Assign "maxSpeed" and "acceleration" in the front GameObject with the Bogie script

8. If the train follows a set route, assign a RoutingTable Object to "routingTable"

When Play is clicked, the train should snap to the track and go to the next waypoint or a random part of the track



## Creating a route using a RoutingTable

1. Create a GameObject for each station and add:
        * Station

2. If there are multiple routes between stations and need more control how the train goes between them, create a GameObject and add:
        * Waypoint

Trains will slow down and stop at stations even if the wait time is 0
Use a waypoint to go straight past a stop

Waypoints and stations draw a line to where on the track they are
Waypoints and stations connect to anchors on the track, if there is no anchor where the waypoint/station needs to be, split the track at that position

3. Create a new routing table by right-clicking assets and clicking: Create > MrWhimble > Routing Table

4. Click the "+" button to add a new waypoint/station

5. Select to waypoint/station for the dropdown next to name

6. Repeat steps 3 and 4 until all required waypoints/station have been added

7. If the route is going the wrong way, change the side

8. Assign the leave condition of the stations
        * if the leave condition is "After Time" then the train will wait for that amount of time before going
        * if the leave condition is "After Event" then the train only leaves once the Go() function is called on the lead bogie

9. Assign the routing table to the lead bogie

### Check the console if the route does not yield a loop