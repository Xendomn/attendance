use serde::{Deserialize, Serialize};

#[crud_table(table_name: "student")]
#[derive(Serialize, Deserialize, Clone, Debug)]
pub struct User {
    pub id: Option<i32>,
    pub name: Option<String>,
    pub age: Option<i32>,
}
