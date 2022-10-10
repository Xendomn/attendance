use serde::{Deserialize, Serialize};

#[derive(Serialize, Deserialize, Clone, Debug)]
pub struct Response<T> {
    pub code: Option<u32>,
    pub body: Option<T>,
}
