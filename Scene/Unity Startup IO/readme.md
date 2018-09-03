### Unity Startup IO

try to solve the problem that it's slow when the scene starts up in unity at first time

--------------------------

delete all `Resources` files may speed up a little. After this step, on the another PC the first start up time will be minimized to few seconds. But on this terriable PC it makes no effect.

--------------------------

use WPA and WPR to analyze, but when it's stuck, only UI delay shows sth, CPU\GPU\memory\storage shows nothing

--------------------------
OHHHH! according to this answer: [First time starting scene extemely slow](https://forum.unity.com/threads/first-time-starting-scene-extemely-slow.498521/), the start up time has been minimized from five mins to half a min!
