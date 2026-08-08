<img width="883" height="637" alt="image" src="https://github.com/user-attachments/assets/3e8db61e-1865-4c52-8240-edaf60a39709" />
An image viewer with metadata display for people who generate images with different tools.

It can display the image, metadata in the "parameters" group and the comfy ui "prompt" and "workflow" sections. The image can be zoomed with the mouse wheel and panned with the scrollbars (some functionality is missing here still, like proper mouse panning). 

Buttons on top let the user open a directory, cycle through the images within ("prev" and "next"), delete files to the recycle bin (not directly deleting as safety measure), as well as set image to 1:1 or "fit to area".

There is a file counter in the statusbar which shows current file and total files in directory. The files are checked by a thread, so if files are deleted or added the counter is updated and shows the right amount and location (index+1).

This tool is not supposed to be an image editor or tool for minor manipulations, it is just a viewer with delete functionality...
