   M S B A M L   `   `   `  ����I  ECCT.NUI.Visual, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullG4clr-namespace:CCT.NUI.Visual;assembly=CCT.NUI.VisualCCT.NUI.Visual  5   	    Sign To Code#   CCT.NUI.SignToCode.MainWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   Cuc4clr-namespace:CCT.NUI.Visual;assembly=CCT.NUI.Visual     5   	   -   5          � Title$  Sign To Code: Main Menu��5   	   $
��1080��6)   $
��1920��67    � Icon$ SignToCode.ico��6D    J� IsManipulationEnabled .  � WindowState$ 	Maximized=�6�   .��5      � $��	10,0,2,-3q�6    � RowDefinitions 5   
   �� 5      $
��158* �6   �� 5	      $
��131* �6   .+�5   
   �� $��Center=�6   $	��416��62   $��0,0,0,10q�6>   $��1��6P   .+�5      �� -   5         ��Name$ codeModeBtn��5      $
��58,5q�6*   $��Bottom=�68   $��Center=�6S   $	��300��6�   $��62��6�   ���	#FF4E1677$��Center=�6�   $��Bottom=�6�   .��5      �� $	��302��6   $��62��6)   .+�5      �� $��40(�6   
�������$��
Myriad Pro)�6=   $��SemiBold"�6U   $��5,5,93,0q�6k   $��	Code Mode��6}   $
��Left=�6�   �� 5      $��62��6   $��Images/Code_Mode.png��6(   $��80��6F   $��Right=�6Q   $��	0,-60,5,0q�6m   5      5      �� 5      -   5        $ exitBtn��5      $��
58,27,58,5q�6&   $��Bottom=�6:   $��Center=�6U   $	��301��6�   $��62��6�   ���	#FF4E1677$��Center=�6�   $��Bottom=�6�   .��5      �� $	��302��6   $��62��6)   .+�5      �� $��40(�6   
�������$��
Myriad Pro)�6=   $��SemiBold"�6U   $��5,5,93,0q�6k   $
��Exit��6}   $
��Left=�6�   �� 5      $��62��6   $��Images/Exit.png��6(   $��80��6A   $��Right=�6L   $��	0,-60,5,0q�6h   5      5      �� 5      -   5         ��Name$ Sign_To_Code��5      $��Sign To Code��6*   ���	#FF4E1677$��Center=�6X   $��Center=�6u   $��
Myriad Pro)�6�   $��72(�6�   $
��Bold"�6�   $��	0,195,0,0q�6�   5      �� 5   
   $	��604��6   $	��404��6   $��Center=�6*   $��0,72,0,0q�6G   $	��Top=�6Y   ���	#FF4E1677$��5q�6�   ._�5      $   CCT.NUI.Visual.WpfVideoControl  -   5          Name$ videoControl��5   !   $��0,0,0,0q�67   5      5      5      