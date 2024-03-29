use std::ffi::c_void;

use netcorehost::{nethost, pdcstr};

use crate::{FunctionParameter, ReturnValue};

#[derive(Debug, Clone, Copy)]
#[repr(transparent)]
pub struct DotnetFnId(u64);

#[derive(Debug, Clone)]
#[repr(C, u8)]
pub enum DotnetInteropResult<T> {
    Ok(T) = 0,
    Err(DotnetInteropError),
}

impl<T> DotnetInteropResult<T> {
    pub fn unwrap(self) -> T {
        use DotnetInteropResult as dir;
        match self {
            dir::Ok(t) => t,
            dir::Err(err) => panic!("called unwrap on an Err value: {err:?}"),
        }
    }
}

impl<T> From<DotnetInteropResult<T>> for Result<T, DotnetInteropError> {
    fn from(value: DotnetInteropResult<T>) -> Self {
        match value {
            DotnetInteropResult::Ok(t) => Ok(t),
            DotnetInteropResult::Err(err) => Err(err),
        }
    }
}

#[derive(Debug, Clone)]
#[repr(i32)]
pub enum DotnetInteropError {
    StringIsNull = 0,
    MethodNotFound,
    NotImplemented,
    ReturnTypeNotSupported,
}

#[derive(Debug)]
#[repr(C)]
pub struct SpisharpDotnetCallbacks {
    pub create_static_function_id: unsafe extern "system" fn(
        libstate: *mut c_void,
        fully_qualified_type_name_ptr: *const u8,
        fully_qualified_type_name_length: usize,
        method_name_ptr: *const u8,
        method_name_length: usize,
    )
        -> DotnetInteropResult<DotnetFnId>,

    pub call_static_function: unsafe extern "system" fn(
        libstate: *mut c_void,
        fn_id: DotnetFnId,
        params_array: *const FunctionParameter,
        params_array_len: usize,
    ) -> DotnetInteropResult<ReturnValue>,
}

impl Default for SpisharpDotnetCallbacks {
    fn default() -> Self {
        initialize_hostfxr_and_create_spisharp_callbacks()
    }
}

pub fn initialize_hostfxr_and_create_spisharp_callbacks() -> SpisharpDotnetCallbacks {
    let hostfxr = nethost::load_hostfxr().unwrap();
    let context = hostfxr
        .initialize_for_runtime_config(pdcstr!("SpisharpCS.runtimeconfig.json"))
        .unwrap();
    let fn_loader = context
        .get_delegate_loader_for_assembly(pdcstr!("SpisharpCS.dll"))
        .unwrap();
    let entrypoint = fn_loader
        .get_function_with_unmanaged_callers_only::<extern "system" fn() -> SpisharpDotnetCallbacks>(pdcstr!("SpisharpCS.UnmanagedEntrypoint, SpisharpCS"), pdcstr!("CreateUnmanagedFunctionPointers")).unwrap();
    entrypoint()
}
