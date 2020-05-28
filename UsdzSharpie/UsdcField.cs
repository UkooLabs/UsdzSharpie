using System;
using System.IO;

namespace UsdzSharpie
{
    public struct UsdcField
    {
        public enum ValueTypeId
        {
            ValueTypeInvaldOrUnsupported = 0,
            ValueTypeBool = 1,
            ValueTypeUChar = 2,
            ValueTypeInt = 3,
            ValueTypeUInt = 4,
            ValueTypeInt64 = 5,
            ValueTypeUInt64 = 6,
            ValueTypeHalf = 7,
            ValueTypeFloat = 8,
            ValueTypeDouble = 9,
            ValueTypeString = 10,
            ValueTypeToken = 11,
            ValueTypeAssetPath = 12,
            ValueTypeMatrix2d = 13,
            ValueTypeMatrix3d = 14,
            ValueTypeMatrix4d = 15,
            ValueTypeQuatd = 16,
            ValueTypeQuatf = 17,
            ValueTypeQuath = 18,
            ValueTypeVec2d = 19,
            ValueTypeVec2f = 20,
            ValueTypeVec2h = 21,
            ValueTypeVec2i = 22,
            ValueTypeVec3d = 23,
            ValueTypeVec3f = 24,
            ValueTypeVec3h = 25,
            ValueTypeVec3i = 26,
            ValueTypeVec4d = 27,
            ValueTypeVec4f = 28,
            ValueTypeVec4h = 29,
            ValueTypeVec4i = 30,
            ValueTypeDictionary = 31,
            ValueTypeTokenListOp = 32,
            ValueTypeStringListOp = 33,
            ValueTypePathListOp = 34,
            ValueTypeReferenceListOp = 35,
            ValueTypeIntListOp = 36,
            ValueTypeInt64ListOp = 37,
            ValueTypeUIntListOp = 38,
            ValueTypeUInt64ListOp = 39,
            ValueTypePathVector = 40,
            ValueTypeTokenVector = 41,
            ValueTypeSpecifier = 42,
            ValueTypePermission = 43,
            ValueTypeVariability = 44,
            ValueTypeVariantSelectionMap = 45,
            ValueTypeTimeSamples = 46,
            ValueTypePayload = 47,
            ValueTypeDoubleVector = 48,
            ValueTypeLayerOffsetVector = 49,
            ValueTypeStringVector = 50,
            ValueTypeValueBlock = 51,
            ValueTypeValue = 52,
            ValueTypeUnregisteredValue = 53,
            ValueTypeUnregisteredValueListOp = 54,
            ValueTypePayloadListOp = 55,
            ValueTypeTimeCode = 56
        };
        public enum SpecType
        {
            SpecTypeUnknown = 0,
            SpecTypeAttribute,
            SpecTypeConnection,
            SpecTypeExpression,
            SpecTypeMapper,
            SpecTypeMapperArg,
            SpecTypePrim,
            SpecTypePseudoRoot,
            SpecTypeRelationship,
            SpecTypeRelationshipTarget,
            SpecTypeVariant,
            SpecTypeVariantSet,
            NumSpecTypes
        };

        public enum Orientation
        {
            OrientationRightHanded, // 0
            OrientationLeftHanded,
        };

        public enum Visibility
        {
            VisibilityInherited, // 0
            VisibilityInvisible,
        };

        public enum Purpose
        {
            PurposeDefault, // 0
            PurposeRender,
            PurposeProxy,
            PurposeGuide,
        };

        public enum SubdivisionScheme
        {
            SubdivisionSchemeCatmullClark, // 0
            SubdivisionSchemeLoop,
            SubdivisionSchemeBilinear,
            SubdivisionSchemeNone,
        };

        // For PrimSpec
        public enum Specifier
        {
            SpecifierDef,  // 0
            SpecifierOver,
            SpecifierClass,
            NumSpecifiers
        };

        public enum Permission
        {
            PermissionPublic,  // 0
            PermissionPrivate,
            NumPermissions
        };

        public enum Variability
        {
            VariabilityVarying,  // 0
            VariabilityUniform,
            VariabilityConfig,
            NumVariabilities
        };



        public string Name;

        public ulong Flags;

        public ValueTypeId Type
        {
            get
            {
                var value = (Flags >> 48) & 0xff;
                return value.ToEnum<ValueTypeId>();
            }
        }

        public bool IsArray => (Flags & ((ulong)1 << 63)) > 0;

        public bool IsInlined => (Flags & ((ulong)1 << 62)) > 0;

        public bool IsCompressed => (Flags & ((ulong)1 << 61)) > 0;

        public ulong Payload => (Flags & ((ulong)1 << 48) - 1);


    }
}
