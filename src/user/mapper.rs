use crate::{user::bean::user::User, RB};
use rbatis::crud::CRUD;
use rbatis::db::DBExecResult;

pub async fn save(user: &User) -> Result<DBExecResult, rbatis::Error> {
    RB.save(user, &[]).await
}
