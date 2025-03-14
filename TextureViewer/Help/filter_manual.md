# Filter Manual

Filters are simple GLSL compute shader without the #version and local_size specification.

## Additional Preprocessor directives:
### Settings:

**#setting** title, *example title*

Sets the title of the shader that will be displayed in the filter list

**#setting** description, *example description*

Sets the description of the shader that will be displayed in the filter tab

**#setting** sepa, *true/false*

Specifies if the shader is a seperatable shader. If sepa is set to true, the shader will be executed twice. In the first run, the unform variable "ivec2 filterDirection" will be set to ivec2(1,0). In the second run, the variable will be set to ivec2(0,1). The default value is false.

**#settings** singleinvocation, *true/false*

Specifies if the shader is called with a single invocation. Set this value to
false if the shader takes several seconds to complete in order to avoid application crashes.
The default value is true.

### Parameters:

To set uniform variables from the filter tab, you have to specify parameters.
The syntax is:

**#param** *Displayed Name*, *Variable Name*, *Type*, *DefaultValue* [, *Minimun* [, *Maximum*]]

*Displayed Name*: Will be displayed in the filter tab as variable name.

*Variable Name*: Name of the shader variable.

*Type*: type of the variable. Valid types are: Int, Float, Bool.

*DefaultValue*: Initial value of the variable.

*Minimum*: (Optional) Minimum allowed value of the variable.

*Maximum*: (Optional) Maximum allowed value of the variable.

Additional properties can be specified via:
**#paramprop** *Displayed Name*, *Action*, ...

*Displayed Name*: Name of the affected parameter (same as **#parameter** name)
*Action*: Event that happened. Currently following actions are defined:
    
- OnAdd: this action will be activated when the up button on the property number box (in the filter tab) is pushed. Aditional parameters are Value, Operation. See Keybindings for an explanation.
    
- OnSubtract: this action will be activated when the down button on the property number box (in the filter tab) is pushed. Aditional parameters are Value, Operation. See Keybindings for an explanation.

***Example:***

`#param Gamma, gma, Float, 1.0, 0.0`

`#paramprop Gamma, onAdd, 2.0, multiply`

`float a = pow(0.5, gma); // variable usage`

### Texture Parameters:

The original (imported) images can be accessed by using the **#texture** directive:

**#texture** *Displayed Name*, *Function Name*

*Displayed Name*: Will be displayed in the filter tab as texture name.

*Function Name*: Name of the function that will be provided to access the texture data. Two Functions will be provided:
* `vec4 FunctionName(ivec2 coord)` returns pixel color for `coord` in pixel coordinates.
* `vec4 FunctionName(vec2 coord)` returns interpolated pixel color for `coord` in (0, 1) coordinates.
 
The desired (imported) texture can be selected in the filter menu.
 
***Example:***

`#texture Normal Texture, NormalTex`

`vec4 firstPixel = NormalTex(ivec2(0, 0));`

## Inputs and Outputs:

The shader will be called per pixel of the image. The pixel positon can be determined
with: `ivec2 pixelCoord = ivec2(gl_GlobalInvocationID.xy) + pixelOffset;`

### Source and Destination Image:

The source image can be accesed via `uniform sampler2D src_image`.
level of detail should be 0 (for texelFetch etc.)
access with texture(...) will give linear interpolated values

The destination image can be accesed via `writeonly image2D dst_image`.

## Keybindings

To quickly change parameters within the application you can create keybindings.

**#keybinding** *Displayed Name*, *Keycode*, *Value*, *Operation*

*Displayed Name*: Name of the affected parameter (same as *#parameter* name).

*Keycode*: C# keycode for the corresponding keybinding.

*Value*: (decimal) value to modify the old parameter.

*Operation*: how to modify the parameter. Valid types: add, multiply, set.

When pressing the key, the new parameter value will be: parameterValue (operation) Value

***Example:***

`#keybinding Gamma, P, 0.5, multiply`

=> after pressing P the gamma value will be multiplied by 0.5

`#keybinding Gamma, I, 10.0, set`

=> after pressing I the gamma value will be set to 10.0

More examples:

* See gamma.comp for a simple example.
* See blur.comp for a simple seperatable shader example (Gaussian Blur) 
* See silhouette.comp for an example with texture bindings.
