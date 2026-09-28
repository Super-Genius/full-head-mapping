bl_info = {
    "name": "Super-Genius Full Head Mapping Helper",
    "author": "Kenneth Hurley / Super-Genius",
    "version": (0, 1, 0),
    "blender": (4, 3, 0),
    "location": "View3D > Sidebar > Full Head Mapping",
    "description": "Set up photographic projector cameras and export a capture manifest for the Full Head Mapping workflow.",
    "category": "UV",
}

import bpy
import json
import math
import os
from mathutils import Vector


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()


class FHM_OT_create_projectors(bpy.types.Operator):
    bl_idname = "fhm.create_projectors"
    bl_label = "Create 4 Projectors"
    bl_options = {'REGISTER', 'UNDO'}

    def execute(self, context):
        target = context.active_object
        if target is None or target.type != 'MESH':
            self.report({'ERROR'}, "Select the head mesh first")
            return {'CANCELLED'}

        corners = [target.matrix_world @ Vector(corner) for corner in target.bound_box]
        center = sum(corners, Vector()) / 8.0
        radius = max((p - center).length for p in corners)
        distance = max(radius * 3.0, 1.0)

        setups = [
            ("Front", Vector((0, -distance, 0))),
            ("Left", Vector((-distance, 0, 0))),
            ("Right", Vector((distance, 0, 0))),
            ("Back", Vector((0, distance, 0))),
        ]

        collection = bpy.data.collections.get("FHM Projectors")
        if collection is None:
            collection = bpy.data.collections.new("FHM Projectors")
            context.scene.collection.children.link(collection)

        for label, offset in setups:
            data = bpy.data.cameras.new("FHM " + label)
            data.lens = 55.0
            obj = bpy.data.objects.new("FHM Projector - " + label, data)
            collection.objects.link(obj)
            obj.location = center + offset
            look_at(obj, center)
            obj["fhm_label"] = label
            obj["fhm_image"] = ""

        self.report({'INFO'}, "Created front/left/right/back projector cameras")
        return {'FINISHED'}


class FHM_OT_assign_image(bpy.types.Operator):
    bl_idname = "fhm.assign_image"
    bl_label = "Assign Image to Active Camera"
    filepath: bpy.props.StringProperty(subtype='FILE_PATH')

    def invoke(self, context, event):
        context.window_manager.fileselect_add(self)
        return {'RUNNING_MODAL'}

    def execute(self, context):
        camera_obj = context.active_object
        if camera_obj is None or camera_obj.type != 'CAMERA':
            self.report({'ERROR'}, "Select an FHM camera first")
            return {'CANCELLED'}

        path = bpy.path.abspath(self.filepath)
        if not os.path.exists(path):
            self.report({'ERROR'}, "Image file does not exist")
            return {'CANCELLED'}

        image = bpy.data.images.load(path, check_existing=True)
        camera_obj.data.show_background_images = True
        bg = camera_obj.data.background_images.new()
        bg.image = image
        bg.display_depth = 'FRONT'
        bg.alpha = 0.65
        camera_obj["fhm_image"] = path
        return {'FINISHED'}


class FHM_OT_export_manifest(bpy.types.Operator):
    bl_idname = "fhm.export_manifest"
    bl_label = "Export Capture Manifest"
    filepath: bpy.props.StringProperty(subtype='FILE_PATH', default="full_head_capture.json")

    def invoke(self, context, event):
        if not self.filepath.lower().endswith('.json'):
            self.filepath += '.json'
        context.window_manager.fileselect_add(self)
        return {'RUNNING_MODAL'}

    def execute(self, context):
        entries = []
        for obj in bpy.data.objects:
            if obj.type != 'CAMERA' or "fhm_label" not in obj:
                continue
            matrix = obj.matrix_world
            entries.append({
                "label": obj.get("fhm_label", obj.name),
                "image": obj.get("fhm_image", ""),
                "lens_mm": obj.data.lens,
                "sensor_width_mm": obj.data.sensor_width,
                "matrix_world_row_major": [matrix[r][c] for r in range(4) for c in range(4)],
            })

        payload = {
            "format": "super-genius-full-head-mapping-capture-v1",
            "coordinate_system": "Blender right-handed Z-up; conversion is required for Unity",
            "views": entries,
        }
        with open(bpy.path.abspath(self.filepath), 'w', encoding='utf-8') as f:
            json.dump(payload, f, indent=2)
        self.report({'INFO'}, f"Exported {len(entries)} capture views")
        return {'FINISHED'}


class FHM_PT_panel(bpy.types.Panel):
    bl_label = "Full Head Mapping"
    bl_idname = "FHM_PT_panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'Full Head Mapping'

    def draw(self, context):
        layout = self.layout
        layout.label(text="Projector setup")
        layout.operator("fhm.create_projectors")
        layout.operator("fhm.assign_image")
        layout.separator()
        layout.label(text="Interchange")
        layout.operator("fhm.export_manifest")
        layout.separator()
        layout.label(text="Use Blender UV Project for")
        layout.label(text="preview/alignment; use Unity")
        layout.label(text="baker for multiband output.")


CLASSES = (
    FHM_OT_create_projectors,
    FHM_OT_assign_image,
    FHM_OT_export_manifest,
    FHM_PT_panel,
)


def register():
    for cls in CLASSES:
        bpy.utils.register_class(cls)


def unregister():
    for cls in reversed(CLASSES):
        bpy.utils.unregister_class(cls)


if __name__ == "__main__":
    register()
