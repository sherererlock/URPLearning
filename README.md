# URPLearning

#### Stencil例子描述



1. 渲染蒙版

   蒙版渲染过程中会将卡牌区域的Stencil Buffer的值更新为1，其他区域的值更新为0

           ColorMask 0 
           ZWrite off
           Stencil
           {
               Ref [_ID]
               Comp always
               Pass replace
           }

   不写颜色，不写深度，只写stencilbuffer，且一直通过

2. 渲染场景中的物体

   物体渲染过程中，会比较Stencil Buffer的值和物体材质的的Mask值，如果相等，则蒙版测试通过，如果不等，则蒙版测试不通过。卡牌区域的蒙版值相等，其他区域的值不相等，所以只渲染出卡牌区域的值

           stencil
           {
               Ref[_ID]
               Comp equal
           }

