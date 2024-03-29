use std::{collections::HashMap, error::Error, ffi::c_void, fmt::Display};

use dotnet::{DotnetFnId, DotnetInteropError, SpisharpDotnetCallbacks};
use jni::{
    objects::{GlobalRef, JClass, JMethodID, JObject, JStaticMethodID},
    signature::ReturnType,
    sys::jvalue,
    JNIEnv,
};

pub mod dotnet;

#[cfg(test)]
mod tests;

#[derive(Debug, Default)]
pub struct LibState {
    java_static_fns: HashMap<JavaFnId, JStaticMethodID>,
    java_methods: HashMap<JavaFnId, JMethodID>,
    java_objs: HashMap<ObjectId, GlobalRef>,
    java_ref: Option<GlobalRef>,
    dotnet_callbacks: SpisharpDotnetCallbacks,
}

impl LibState {
    pub fn create() -> Box<LibState> {
        Box::new(LibState::default())
    }

    pub unsafe fn call_static_java_fn<'local>(
        &self,
        mut env: JNIEnv<'local>,
        class: &JClass,
        fn_id: JavaFnId,
        ret_type: ReturnType,
        args: &[jvalue],
    ) -> Result<Option<FunctionParameter>, NotFoundError> {
        let method_id = self
            .java_static_fns
            .get(&fn_id)
            .ok_or_else(|| NotFoundError("Unable to find static method with id".to_owned()))?;
        let _return_value = env
            .call_static_method_unchecked(class, method_id, ret_type, args)
            .expect("error calling call_static_method_unchecked");
        Ok(None) // TODO: return value
    }

    pub unsafe fn call_java_method_fn<'local>(
        &self,
        mut env: JNIEnv<'local>,
        obj: ObjectId,
        fn_id: JavaFnId,
        ret_type: ReturnType,
        args: &[jvalue],
    ) -> Result<Option<FunctionParameter>, NotFoundError> {
        let object: &JObject = self
            .java_objs
            .get(&obj)
            .ok_or_else(|| NotFoundError("C# object not synced to Java".to_owned()))?
            .as_ref();
        let method_id = self
            .java_methods
            .get(&fn_id)
            .ok_or_else(|| NotFoundError("Unable to find static method with id".to_owned()))?;
        let _return_value = env
            .call_method_unchecked(object, method_id, ret_type, args)
            .expect("Unable to call java method");
        Ok(None)
    }

    pub fn create_static_dotnet_function_id(
        &mut self,
        fully_qualified_type_name: &str,
        method_name: &str,
    ) -> Result<DotnetFnId, DotnetInteropError> {
        // this is safe because we are taking a pointer to the string and passing the length which we know is valid
        unsafe {
            (self.dotnet_callbacks.create_static_function_id)(
                self as *mut _ as *mut c_void,
                fully_qualified_type_name.as_ptr(),
                fully_qualified_type_name.len(),
                method_name.as_ptr(),
                method_name.len(),
            )
        }
        .into()
    }

    pub fn call_static_dotnet_func(
        &mut self,
        func_id: DotnetFnId,
        params: &[FunctionParameter],
    ) -> Result<ReturnValue, DotnetInteropError> {
        // safe because slice should be correct
        unsafe {
            (self.dotnet_callbacks.call_static_function)(
                self as *mut _ as *mut c_void,
                func_id,
                params.as_ptr(),
                params.len(),
            )
        }
        .into()
    }
}

#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
#[repr(transparent)]
pub struct ObjectId(u64);

#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
#[repr(transparent)]
pub struct JavaFnId(u64);

#[derive(Debug)]
pub struct NotFoundError(String);

impl Display for NotFoundError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        f.write_str(self.0.as_str())
    }
}

impl Error for NotFoundError {}

#[derive(Debug, Clone, PartialEq)]
#[repr(C, u8)]
pub enum FunctionParameter {
    Byte(i8) = 0,
    Short(i16),
    Int(i32),
    Long(i64),
    Bool(bool),
    Float(f32),
    Double(f64),
    Char(u16),
    Obj(ObjectId),
}

#[derive(Debug, Clone, PartialEq)]
#[repr(C, u8)]
pub enum ReturnValue {
    None,
    Byte(i8),
    Short(i16),
    Int(i32),
    Long(i64),
    Bool(bool),
    Float(f32),
    Double(f64),
    Char(u16),
    Obj(ObjectId),
}

pub mod java {
    use std::mem::size_of;

    use jni::{
        objects::{JByteBuffer, JClass, JValue},
        sys::{jboolean, jbyte, jchar, jdouble, jfloat, jint, jlong, jshort, jvalue, JNI_TRUE},
        JNIEnv,
    };
    use jni_mangle::mangle;

    use crate::{FunctionParameter, LibState};

    #[mangle(
        package = "co.tantleffbeef.spisharp.interop",
        class = "LibState",
        method = "create"
    )]
    fn libstate_create<'local>(env: JNIEnv<'local>, _class: JClass<'local>) -> JByteBuffer<'local> {
        let mut env = env;
        let state = LibState::create();
        let state_ptr = Box::into_raw(state);
        unsafe {
            let buffer = env
                .new_direct_byte_buffer(state_ptr as *mut u8, size_of::<LibState>())
                .unwrap();
            let buffer_ref = env
                .new_global_ref(&buffer)
                .expect("unable to create global ref of byte buffer");
            (*state_ptr).java_ref = Some(buffer_ref);

            buffer
        }
    }

    #[mangle(
        package = "co.tantleffbeef.spisharp.interop",
        class = "LibState",
        method = "destroy"
    )]
    fn libstate_destroy(env: JNIEnv, _class: JClass, ptr_buf: JByteBuffer) {
        let libstate_ptr = env
            .get_direct_buffer_address(&ptr_buf)
            .expect("Couldn't get address of buffer");
        let libstate = unsafe { Box::from_raw(libstate_ptr as *mut LibState) };
        drop(libstate)
    }

    #[mangle(
        package = "co.tantleffbeef.spisharp.interop",
        class = "Parameter",
        method = "createByte"
    )]
    fn parameter_create_byte(_env: JNIEnv, _class: JClass, value: jbyte) -> jvalue {
        let param = Box::new(FunctionParameter::Byte(value));
        let ptr = Box::into_raw(param);
        unsafe {
            jvalue {
                j: std::mem::transmute(ptr),
            }
        }
    }

    #[mangle(
        package = "co.tantleffbeef.spisharp.interop",
        class = "Parameter",
        method = "createShort"
    )]
    fn parameter_create_short(
        _env: JNIEnv,
        _class: JClass,
        value: jshort,
    ) -> Box<FunctionParameter> {
        Box::new(FunctionParameter::Short(value))
    }

    #[mangle(
        package = "co.tantleffbeef.spisharp.interop",
        class = "Parameter",
        method = "createInt"
    )]
    fn parameter_create_int(_env: JNIEnv, _class: JClass, value: jint) -> Box<FunctionParameter> {
        Box::new(FunctionParameter::Int(value))
    }

    #[mangle(
        package = "co.tantleffbeef.spisharp.interop",
        class = "Parameter",
        method = "createLong"
    )]
    fn parameter_create_long(_env: JNIEnv, _class: JClass, value: jlong) -> Box<FunctionParameter> {
        Box::new(FunctionParameter::Long(value))
    }

    #[mangle(
        package = "co.tantleffbeef.spisharp.interop",
        class = "Parameter",
        method = "createBool"
    )]
    fn parameter_create_bool(
        _env: JNIEnv,
        _class: JClass,
        value: jboolean,
    ) -> Box<FunctionParameter> {
        if value == JNI_TRUE {
            Box::new(FunctionParameter::Bool(true))
        } else {
            Box::new(FunctionParameter::Bool(false))
        }
    }

    #[mangle(
        package = "co.tantleffbeef.spisharp.interop",
        class = "Parameter",
        method = "createFloat"
    )]
    fn parameter_create_float(
        _env: JNIEnv,
        _class: JClass,
        value: jfloat,
    ) -> Box<FunctionParameter> {
        Box::new(FunctionParameter::Float(value))
    }

    #[mangle(
        package = "co.tantleffbeef.spisharp.interop",
        class = "Parameter",
        method = "createDouble"
    )]
    fn parameter_create_double(
        _env: JNIEnv,
        _class: JClass,
        value: jdouble,
    ) -> Box<FunctionParameter> {
        Box::new(FunctionParameter::Double(value))
    }

    #[mangle(
        package = "co.tantleffbeef.spisharp.interop",
        class = "Parameter",
        method = "createChar"
    )]
    fn parameter_create_char(_env: JNIEnv, _class: JClass, value: jchar) -> Box<FunctionParameter> {
        Box::new(FunctionParameter::Char(value))
    }
}
